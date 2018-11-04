using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Backup.Runners;
using BackupDatabase.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using Backup.CommandsAttributes;
using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace Backup
{
    public class Program
    {
        private static IConfiguration Configuration { get; set; }
        private static Dictionary<string, Type> CommandsLibrary { get; set; }
        public static Regex splitRegex = new Regex("(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
        public static Regex matchQuotedRegex = new Regex("\"(.*?)\"", RegexOptions.Singleline);
        public static Regex unQuoteRegex = new Regex("[^\"]*[^\"]");

        public static void Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            using (var scope = BuildDependencyContainer().BeginLifetimeScope())
            {
                Run(scope).GetAwaiter().GetResult();
            }
        }

        private static IContainer BuildDependencyContainer()
        {
            var builder = new ContainerBuilder();
            builder
                .RegisterInstance(new BackupDatabase.Cassandra.CassandraMetaDB(Configuration["Database:CassandraMetaIP"]) { PasswordsKey = Encoding.UTF8.GetBytes(Configuration["Encryption:PasswordsKey"]) })
                .As<BackupDatabase.IMetaDBAccess>();

            builder
                .RegisterInstance(new BackupDatabase.Cassandra.CassandraUsersDB(Configuration["Database:CassandraMetaIP"]))
                .As<BackupDatabase.IUsersDBAccess>();


            builder
                .RegisterInstance(Console.Out);

            builder
                .RegisterInstance(Console.In);

        
            // Do some reflection on Namespace "Backup.Commands" to get all commands and actions from classes
            CommandsLibrary = new Dictionary<string, Type>();
            var commandInterface = typeof(Commands.ICommand);
            foreach (var commandType in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass && commandInterface.IsAssignableFrom(type)))
            {
                builder.RegisterType(commandType).As(commandType);
                CommandsLibrary.Add(CommandNameFromAttribute(commandType), commandType);
            }

            
            return builder.Build();
        }

        private static async Task Run(ILifetimeScope scope)
        {

            var command = ReadCommand();

            while (command.command != "exit")
            {
                // First, get the command (aka. first string read)
                if (CommandsLibrary.TryGetValue(command.command, out Type commandLibrary))
                {
                    var commandInstance = (Commands.ICommand)scope.Resolve(commandLibrary);

                    // Second, get the action (aka. second string read)
                    // If no action provided, invoque default one
                    if (command.action == default)
                    {
                        await commandInstance.Default();
                    }
                    else
                    {
                        // Third, the rest is the parameters of the action
                        var argumentsCount = command.arguments.Count();

                        var commandActions = commandInstance.GetType()
                            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                            .Where(method => ActionNameFromAttribute(method) == command.action && argumentsCount >= method.GetParameters().Length);

                        var foundMethod = false;
                        foreach(var commandAction in commandActions)
                        {
                            try
                            {
                                // Will transform "string" parameters from console to typed parameters to invoke methods
                                IEnumerable<object> coercedParameters;
                                var actionParametersInfo = commandAction.GetParameters();

                                // If last parameter is an array, don't process it now
                                if (actionParametersInfo.Length > 0 && actionParametersInfo.Last().ParameterType.IsArray)
                                {
                                    var arrayElementType = actionParametersInfo.Last().ParameterType.GetElementType();
                                    // Coerece the parameters to an array of elements of the apropiate type
                                    // Note at this stage, the array itself is of type object[]
                                    var rest = command.arguments
                                        .Skip(actionParametersInfo.Length - 1)
                                        .Select(param => CoerceParameter(param, arrayElementType)).ToArray();

                                    // Convert the array of type object[] to an array of the apropriate type
                                    // Seems a bit ugly, but did not found a better way ...

                                    var lastParam = (IList)Activator.CreateInstance(actionParametersInfo.Last().ParameterType, new object[] { rest.Length });
                                    coercedParameters = command.arguments
                                        .Take(actionParametersInfo.Length - 1)
                                        .Zip(actionParametersInfo, (param, paramInfo) => CoerceParameter(param, paramInfo.ParameterType))
                                        .Append(lastParam);

                                    for (int i = 0; i < rest.Length; i++)
                                    {
                                        lastParam[i] = rest[i];
                                    }

                                }
                                else
                                {
                                    coercedParameters = command.arguments.Zip(actionParametersInfo, (param, paramInfo) => CoerceParameter(param, paramInfo.ParameterType));
                                }

                                // Finally, invoke the method 
                                if (commandAction.Invoke(commandInstance, coercedParameters.ToArray()) is Task invokeResult)
                                {
                                    // await if necessary
                                    await invokeResult;
                                }

                                foundMethod = true;
                                break;
                            }
                            catch (Exception e){ };

                        }

                        if (!foundMethod)
                        {
                            await commandInstance.PrintUsage();
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Sorry, unknown command");
                }

                command = ReadCommand();
            }


        }

        private static string CommandNameFromAttribute(Type commandClass)
        {
            var attribute = Attribute.GetCustomAttributes(commandClass).FirstOrDefault(attr => attr is CommandAttribute);

            var name = attribute as CommandAttribute;
            if (name == null)
            {
                return commandClass.Name.ToLower();
            }
            else
            {
                return ((CommandAttribute)attribute).Name;
            }
        }

        private static string ActionNameFromAttribute(MethodInfo method)
        {
            var attribute = Attribute.GetCustomAttributes(method).FirstOrDefault(attr => attr is ActionAttribute);

            var name = attribute as ActionAttribute;
            if (name == null)
            {
                return method.Name.ToLower();
            }
            else
            {
                return ((ActionAttribute)attribute).Name;
            }
        }

        private static (string command, string action, IEnumerable<string> arguments) ReadCommand()
        {
            Console.Write("> ");
            var rawInput = Console.ReadLine() ?? "";
            var stringArray = splitRegex.Split(rawInput);

            var command = stringArray.FirstOrDefault();
            var action =
                command == default ? default : stringArray.Skip(1).FirstOrDefault();
            var arguments =
                action == default ? Enumerable.Empty<string>() : stringArray.Skip(2).Select(UnQuoteText);

            return (command, action, arguments);
        }

        private static string UnQuoteText(string text)
        {
            var match = matchQuotedRegex.Match(text);
            if (match.Captures.Count > 0)
            {
                return unQuoteRegex.Match(match.Captures[0].Value).Captures[0].Value;
            }
            else
            {
                return text;
            }

        }

        private static object CoerceParameter(string inputValue, Type requiredType)
        {
            var exceptionMessage = $"Cannnot coerce the input argument {inputValue} to required type {requiredType.Name}";

            object result;
            if (requiredType.IsEnum)
            {
                result = Enum.Parse(requiredType, inputValue);
            }
            else if (requiredType == typeof(Guid))
            {
                result = new Guid(inputValue);
            }
            else
            {
                switch (Type.GetTypeCode(requiredType))
                {
                    case TypeCode.String:
                        result = inputValue;
                        break;
                    case TypeCode.Int16:
                        short number16;
                        if (Int16.TryParse(inputValue, out number16))
                        {
                            result = number16;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.Int32:
                        int number32;
                        if (Int32.TryParse(inputValue, out number32))
                        {
                            result = number32;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.Int64:
                        long number64;
                        if (Int64.TryParse(inputValue, out number64))
                        {
                            result = number64;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.Boolean:
                        bool trueFalse;
                        if (bool.TryParse(inputValue, out trueFalse))
                        {
                            result = trueFalse;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.Byte:
                        byte byteValue;
                        if (byte.TryParse(inputValue, out byteValue))
                        {
                            result = byteValue;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.Char:
                        char charValue;
                        if (char.TryParse(inputValue, out charValue))
                        {
                            result = charValue;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.DateTime:
                        DateTime dateValue;
                        if (DateTime.TryParse(inputValue, out dateValue))
                        {
                            result = dateValue;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.Decimal:
                        Decimal decimalValue;
                        if (Decimal.TryParse(inputValue, out decimalValue))
                        {
                            result = decimalValue;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.Double:
                        Double doubleValue;
                        if (Double.TryParse(inputValue, out doubleValue))
                        {
                            result = doubleValue;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.Single:
                        Single singleValue;
                        if (Single.TryParse(inputValue, out singleValue))
                        {
                            result = singleValue;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.UInt16:
                        UInt16 uInt16Value;
                        if (UInt16.TryParse(inputValue, out uInt16Value))
                        {
                            result = uInt16Value;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.UInt32:
                        UInt32 uInt32Value;
                        if (UInt32.TryParse(inputValue, out uInt32Value))
                        {
                            result = uInt32Value;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    case TypeCode.UInt64:
                        UInt64 uInt64Value;
                        if (UInt64.TryParse(inputValue, out uInt64Value))
                        {
                            result = uInt64Value;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    default:
                        throw new ArgumentException(exceptionMessage);
                }
            }

            return result;
        }

            private static void PrintUsage()
        {
            Console.WriteLine("Please enter file name");
        }

    }
}
