module App exposing (main)

import Html exposing (Html, a, div, h1, i, li, nav, section, span, text, ul)
import Html.Attributes exposing (attribute, class, href)
import Html.Events exposing (onClick)
import Html.Lazy exposing (lazy)
import Login
import Navigation
import ServerDetails
import Servers
import Task
import UrlParser exposing (..)


main : Program Never Model Msg
main =
    Navigation.program NewLocation
        { init = init
        , view = view
        , update = update
        , subscriptions = subscriptions
        }



-- MODEL


type alias Model =
    { location : Maybe RouteDetails
    , servers : Servers.Model
    , currentServer : ServerDetails.Model
    , isLoggedIn : Bool
    , loginModel : Login.Model
    }


init : Navigation.Location -> ( Model, Cmd Msg )
init location =
    let
        ( loginModel, loginCmd ) =
            Login.init

        model =
            { location = parser location
            , servers = Servers.init
            , currentServer = ServerDetails.init
            , isLoggedIn = False
            , loginModel = loginModel
            }
    in
    model ! [ Cmd.map LoginMsg loginCmd ]


type Action
    = None
    | ServersSummary
    | ServersDetails String
    | ServersBackup String
    | ServersAdd


type Route
    = Home
    | BackupsRoute
    | ServersRoute
    | CalendarRoute
    | NotFoundRoute


type alias RouteDetails =
    { route : Route
    , action : Action
    }


parser : Navigation.Location -> Maybe RouteDetails
parser =
    parsePath <|
        map RouteDetails <|
            oneOf
                [ map Home top </> map None top
                , map BackupsRoute (s "backups") </> backupsParser
                , map ServersRoute (s "servers") </> serversParser
                , map CalendarRoute (s "calendar") </> backupsParser
                ]


serversParser : Parser (Action -> a) a
serversParser =
    oneOf
        [ map ServersSummary top
        , map ServersAdd (s "add")
        , map ServersDetails (string </> top)
        , map ServersBackup (string </> s "backup")
        ]


backupsParser : Parser (Action -> a) a
backupsParser =
    oneOf
        [ map ServersSummary top
        , map ServersDetails (string </> top)
        , map ServersBackup (string </> s "backup")
        , map ServersAdd (s "backup")
        ]



-- SUBSCRIPTIONS


subscriptions : Model -> Sub Msg
subscriptions model =
    Sub.map LoginMsg (Login.subscriptions model.loginModel)



-- UPDATE


type Msg
    = NewLocation Navigation.Location
    | ChangeURL String
    | LoginMsg Login.Msg
    | LoggedInMsg (Result String Bool)
    | ServersMsg Servers.Msg
    | ServerDetailsMsg ServerDetails.Msg


routeCmd : Model -> Cmd Msg
routeCmd model =
    case model.location of
        Nothing ->
            Cmd.none

        Just { route, action } ->
            case route of
                ServersRoute ->
                    let
                        auth =
                            Login.jwtData model.loginModel
                    in
                    case action of
                        ServersSummary ->
                            Cmd.map ServersMsg <| Servers.getServers auth

                        ServersDetails id ->
                            Cmd.map ServerDetailsMsg <| ServerDetails.getServer id auth

                        _ ->
                            Cmd.none

                _ ->
                    Cmd.none


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        NewLocation location ->
            let
                newModel =
                    { model
                        | location =
                            parser location
                    }

                command =
                    routeCmd newModel
            in
            ( newModel
            , command
            )

        ChangeURL url ->
            ( model, Navigation.newUrl url )

        ServersMsg msg ->
            let
                ( serversModel, serversCmd ) =
                    Servers.update msg model.servers
            in
            ( { model | servers = serversModel }, Cmd.map ServersMsg serversCmd )

        ServerDetailsMsg msg ->
            let
                ( serverModel, serverCmd ) =
                    ServerDetails.update msg model.currentServer
            in
            ( { model | currentServer = serverModel }, Cmd.map ServerDetailsMsg serverCmd )

        LoginMsg msg ->
            let
                ( loginModel, loginCmd ) =
                    Login.update msg model.loginModel
            in
            { model | loginModel = loginModel }
                ! [ Cmd.map LoginMsg loginCmd, Task.attempt LoggedInMsg (Login.isLogged loginModel) ]

        LoggedInMsg (Err msg) ->
            ( model, Cmd.none )

        LoggedInMsg (Ok isLogged) ->
            ( { model | isLoggedIn = isLogged }, routeCmd model )



-- VIEW


view : Model -> Html Msg
view model =
    if model.isLoggedIn then
        div []
            [ section [ class "hero is-primary" ]
                [ div [ class "hero-body" ]
                    [ div [ class "container" ]
                        [ h1 [ class "title" ]
                            [ text "Backups" ]
                        ]
                    ]
                , div [ class "hero-foot" ]
                    [ lazy navbar model
                    ]
                ]
            , nav [ class "navbar has-shadow" ]
                [ div [ class "container" ]
                    [ lazy subNavbar model
                    ]
                ]
            , lazy content model
            ]
    else
        Html.map LoginMsg (Login.loginForm model.loginModel)


isRouteActive : Maybe RouteDetails -> Route -> String
isRouteActive pathname compareRoute =
    case pathname of
        Nothing ->
            ""

        Just { route } ->
            if route == compareRoute then
                " is-active"
            else
                ""


isActionActive : Maybe RouteDetails -> Action -> String
isActionActive pathname compareRoute =
    case pathname of
        Nothing ->
            ""

        Just { action } ->
            if action == compareRoute then
                " is-active"
            else
                ""


navbar : Model -> Html Msg
navbar model =
    let
        isA =
            isRouteActive model.location
    in
    div [ class "tabs is-boxed" ]
        [ div [ class "container" ]
            [ ul []
                [ li [ class <| isA BackupsRoute ]
                    [ a [ onClick (ChangeURL "/backups") ] [ text "Backups" ]
                    ]
                , li [ class <| isA ServersRoute ]
                    [ a [ onClick (ChangeURL "/servers") ] [ text "Servers" ]
                    ]
                , li [ class <| isA CalendarRoute ]
                    [ a [ onClick (ChangeURL "/calendar") ] [ text "Calendar" ]
                    ]
                ]
            ]
        ]


subNavbar : Model -> Html Msg
subNavbar model =
    let
        isA =
            isActionActive model.location
    in
    case model.location of
        Nothing ->
            div [] [ text "Choose a tab" ]

        Just { route } ->
            case route of
                ServersRoute ->
                    div [ class "navbar-tabs" ]
                        [ a [ class <| "navbar-item is-tab" ++ isA ServersSummary, onClick (ChangeURL "/servers") ]
                            [ text "Summary" ]
                        , a [ class <| "navbar-item is-tab" ++ isA ServersAdd, onClick (ChangeURL "/servers/add") ]
                            [ text "Add" ]
                        ]

                _ ->
                    div [ class "navbar-item" ]
                        [ span []
                            [ text "Choose a tab"
                            ]
                        , span [ class "icon is-small" ]
                            [ i [ class "fa fa-level-up" ] []
                            ]
                        ]


content : Model -> Html Msg
content model =
    case model.location of
        Nothing ->
            section [ class "section" ]
                [ div [ class "container" ]
                    [ h1 [] [ text "404 Not Found !" ]
                    ]
                ]

        Just { route, action } ->
            let
                content =
                    case route of
                        ServersRoute ->
                            case action of
                                ServersAdd ->
                                    ServerDetails.add

                                ServersDetails a ->
                                    ServerDetails.details model.currentServer

                                _ ->
                                    Html.map ServersMsg (Servers.summary model.servers)

                        _ ->
                            div [] []
            in
            section [ class "section" ]
                [ div [ class "container" ]
                    [ content
                    ]
                ]
