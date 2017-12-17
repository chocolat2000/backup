module Servers exposing (Model, Msg, getServers, init, summary, update)

import Html exposing (..)
import Html.Attributes exposing (..)
import Html.Events exposing (onClick)
import Html.Keyed
import Http
import Json.Decode as JDecode exposing (..)
import Navigation
import Requests


-- MODEL


type alias Model =
    { servers : List Server
    , isFetching : Bool
    }


type alias Server =
    { id_ : String
    , name : String
    , ip : String
    , port_ : Int
    , type_ : String
    }


init : Model
init =
    { servers = [], isFetching = True }


decodeServersResponse : Decoder (List Server)
decodeServersResponse =
    map5 Server (field "id" JDecode.string) (field "name" JDecode.string) (field "ip" JDecode.string) (field "port" JDecode.int) (field "type" JDecode.string)
        |> JDecode.list



-- UPDATE


type Msg
    = ServersListResponse (Result Http.Error (List Server))
    | ChangeURL String


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        ServersListResponse (Ok list) ->
            ( { model | servers = list, isFetching = False }, Cmd.none )

        ServersListResponse (Err _) ->
            ( model, Cmd.none )

        ChangeURL url ->
            ( model, Navigation.newUrl url )


getServers : String -> Cmd Msg
getServers auth =
    Requests.get ServersListResponse "/api/servers" auth decodeServersResponse



-- VIEW


summary : Model -> Html Msg
summary model =
    if model.isFetching then
        div [ style [ ( "marginBottom", "1.5rem" ) ] ]
            [ i [ class "fa fa-spinner fa-pulse fa-3x fa-fw" ] []
            ]
    else
        table [ class "table is-striped is-hoverable is-fullwidth" ]
            [ thead []
                [ tr []
                    [ th []
                        [ abbr [ attribute "title" "Remove" ] [ text "Rem" ]
                        ]
                    , th [] [ text "Name" ]
                    , th [] [ text "IP / Host" ]
                    , th [] [ text "Type" ]
                    ]
                ]
            , model.servers
                |> List.map
                    summaryLine
                |> Html.Keyed.node "tbody"
                    []
            ]


summaryLine : Server -> ( String, Html Msg )
summaryLine server =
    ( server.id_
    , tr []
        [ th []
            [ button [ class "delete" ] []
            ]
        , td [] [ a [ onClick <| ChangeURL ("/servers/" ++ server.id_) ] [ text server.name ] ]
        , td [] [ text server.ip ]
        , td [] [ text server.type_ ]
        ]
    )
