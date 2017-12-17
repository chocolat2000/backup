module ServerDetails exposing (..)

import Html exposing (..)
import Html.Attributes exposing (..)
import Http
import Json.Decode as JDecode exposing (..)
import Json.Decode.Pipeline as JPipeline exposing (decode, optional, required)
import Requests


-- MODEL


type alias Model =
    { server : Maybe Server
    , isFetching : Bool
    }


type alias LooseServer =
    { id_ : String
    , name : String
    , ip : String
    , port_ : Int
    , type_ : String
    , username : Maybe String
    , password : Maybe String
    }


type alias VMwareFields server =
    { server | thumbPrint : String, vms : List ( String, String ) }


type alias WindowsServer =
    LooseServer


type alias VMwareServer =
    VMwareFields LooseServer


type Server
    = WindowsServer WindowsServer
    | VMwareServer VMwareServer


init : Model
init =
    { server = Nothing, isFetching = True }


decodeWindowsServer : Decoder Server
decodeWindowsServer =
    let
        toWindowsServer id_ name ip port_ type_ username password =
            { id_ = id_, name = name, ip = ip, port_ = port_, type_ = type_, username = username, password = password }
    in
    decode toWindowsServer
        |> JPipeline.required "id" JDecode.string
        |> JPipeline.required "name" JDecode.string
        |> JPipeline.required "ip" JDecode.string
        |> JPipeline.required "port" JDecode.int
        |> JPipeline.required "type" JDecode.string
        |> JPipeline.required "username" (nullable JDecode.string)
        |> JPipeline.required "password" (nullable JDecode.string)
        |> JDecode.map WindowsServer


decodeVMwareServer : Decoder Server
decodeVMwareServer =
    let
        toVMwareServer id_ name ip port_ type_ username password thumbPrint vms =
            { id_ = id_, name = name, ip = ip, port_ = port_, type_ = type_, username = username, password = password, thumbPrint = thumbPrint, vms = vms }
    in
    decode toVMwareServer
        |> JPipeline.required "id" JDecode.string
        |> JPipeline.required "name" JDecode.string
        |> JPipeline.required "ip" JDecode.string
        |> JPipeline.required "port" JDecode.int
        |> JPipeline.required "type" JDecode.string
        |> JPipeline.required "username" (nullable JDecode.string)
        |> JPipeline.required "password" (nullable JDecode.string)
        |> JPipeline.optional "thumbPrint" JDecode.string ""
        |> JPipeline.optional "vms" (JDecode.list <| JDecode.map2 (,) (JDecode.index 0 JDecode.string) (JDecode.index 1 JDecode.string)) []
        |> JDecode.map VMwareServer


decodeServerResponse : Decoder Server
decodeServerResponse =
    field "type" JDecode.string
        |> JDecode.andThen decodeServerType


decodeServerType : String -> Decoder Server
decodeServerType type_ =
    case type_ of
        "Windows" ->
            decodeWindowsServer

        "VMware" ->
            decodeVMwareServer

        _ ->
            JDecode.fail <|
                "Unknown server type: "
                    ++ type_



-- UPDATE


type Msg
    = ServerResponse (Result Http.Error Server)


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        ServerResponse (Ok server) ->
            ( { model | server = Just server, isFetching = False }, Cmd.none )

        ServerResponse (Err _) ->
            ( { model | server = Nothing, isFetching = False }, Cmd.none )


getServer : String -> String -> Cmd Msg
getServer id auth =
    Requests.get ServerResponse ("/api/servers/" ++ id) auth decodeServerResponse



-- VIEW


details : Model -> Html msg
details { server, isFetching } =
    if isFetching then
        div [ style [ ( "marginBottom", "1.5rem" ) ] ]
            [ i [ class "fa fa-spinner fa-pulse fa-3x fa-fw" ] []
            ]
    else
        case server of
            Nothing ->
                h1 [] [ text "server not found !" ]

            Just server_ ->
                let
                    ( content, name, type_ ) =
                        case server_ of
                            WindowsServer s ->
                                ( windowsForm s, s.name, s.type_ )

                            VMwareServer s ->
                                ( vmwareForm s, s.name, s.type_ )
                in
                div [ class "card" ]
                    [ div [ class "card-header" ]
                        [ div [ class "card-header-title" ] [ text (name ++ " - " ++ type_) ]
                        ]
                    , div [ class "card-content" ]
                        [ content
                        ]
                    ]


add : Html msg
add =
    section [ class "section" ]
        [ div [ class "container" ]
            [ div [ class "card" ]
                [ div [ class "card-header" ]
                    [ div [ class "card-header-title" ] [ text "Add new server" ]
                    ]
                , div [ class "card-content" ]
                    [ div [ class "field is-horizontal" ]
                        [ div [ class "field-label is-normal" ]
                            [ label [ class "label" ] [ text "Server type" ]
                            ]
                        , div [ class "field-body" ]
                            [ div [ class "field is-narrow" ]
                                [ div [ class "control" ]
                                    [ div [ class "select is-fullwidth" ]
                                        [ select []
                                            [ option [] [ text "Choose..." ]
                                            , option [] [ text "Windows" ]
                                            , option [] [ text "VMware" ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]


windowsForm : WindowsServer -> Html msg
windowsForm server =
    Html.form []
        [ div [ class "field is-horizontal" ]
            [ div [ class "field-label is-normal" ]
                [ label [ class "label" ] [ text "Name" ]
                ]
            , div [ class "field-body" ]
                [ div [ class "field" ]
                    [ p [ class "control" ]
                        [ input [ class "input", type_ "text", defaultValue server.name ] []
                        ]
                    ]
                ]
            ]
        , div [ class "field is-horizontal" ]
            [ div [ class "field-label is-normal" ]
                [ label [ class "label" ] [ text "Address" ]
                ]
            , div [ class "field-body" ]
                [ div [ class "field" ]
                    [ p [ class "control" ]
                        [ input [ class "input", type_ "text", defaultValue server.ip, placeholder "Ip or DNS" ] []
                        ]
                    ]
                ]
            ]
        , div [ class "field is-horizontal" ]
            [ div [ class "field-label is-normal" ]
                [ label [ class "label" ] [ text "Credentials" ]
                ]
            , div [ class "field-body" ]
                [ div [ class "field" ]
                    [ p [ class "control" ]
                        [ input [ class "input", type_ "text", defaultValue <| Maybe.withDefault "" server.username, placeholder "Username" ] []
                        ]
                    ]
                , div [ class "field" ]
                    [ p [ class "control" ]
                        [ input [ class "input", type_ "password", defaultValue <| Maybe.withDefault "" server.password, placeholder "Password" ] []
                        ]
                    ]
                ]
            ]
        , div [ class "field is-horizontal" ]
            [ div [ class "field-label" ]
                []
            , div [ class "field-body" ]
                [ div [ class "field is-grouped" ]
                    [ p [ class "control" ]
                        [ button [ class "button is-primary" ] [ text "Save" ]
                        ]
                    ]
                ]
            ]
        ]


vmwareForm : VMwareServer -> Html msg
vmwareForm server =
    Html.form []
        [ div [ class "field is-horizontal" ]
            [ div [ class "field-label is-normal" ]
                [ label [ class "label" ] [ text "Name" ]
                ]
            , div [ class "field-body" ]
                [ div [ class "field" ]
                    [ p [ class "control" ]
                        [ input [ class "input", type_ "text", defaultValue server.name ] []
                        ]
                    ]
                ]
            ]
        , div [ class "field is-horizontal" ]
            [ div [ class "field-label is-normal" ]
                [ label [ class "label" ] [ text "vCenter" ]
                ]
            , div [ class "field-body" ]
                [ div [ class "field" ]
                    [ p [ class "control" ]
                        [ input [ class "input", type_ "text", defaultValue server.ip, placeholder "Ip or DNS" ] []
                        ]
                    ]
                ]
            ]
        , div [ class "field is-horizontal" ]
            [ div [ class "field-label is-normal" ]
                [ label [ class "label" ] [ text "Credentials" ]
                ]
            , div [ class "field-body" ]
                [ div [ class "field" ]
                    [ p [ class "control" ]
                        [ input [ class "input", type_ "text", defaultValue <| Maybe.withDefault "" server.username, placeholder "Username" ] []
                        ]
                    ]
                , div [ class "field" ]
                    [ p [ class "control" ]
                        [ input [ class "input", type_ "password", defaultValue <| Maybe.withDefault "" server.password, placeholder "Password" ] []
                        ]
                    ]
                ]
            ]
        , div [ class "field is-horizontal" ]
            [ div [ class "field-label" ]
                []
            , div [ class "field-body" ]
                [ div [ class "field is-grouped" ]
                    [ p [ class "control" ]
                        [ button [ class "button is-primary" ] [ text "Save" ]
                        ]
                    ]
                ]
            ]
        ]
