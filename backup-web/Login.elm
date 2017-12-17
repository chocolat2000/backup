module Login exposing (..)

import AppPorts exposing (..)
import Date
import Date.Extra.Compare as DateCompare
import Html exposing (Html, button, div, form, h1, input, p, section, text)
import Html.Attributes exposing (attribute, style, type_)
import Html.Events exposing (onInput, onSubmit)
import Http
import Json.Decode as JDecode exposing (..)
import Json.Encode as JEncode
import Task


-- MODEL


type alias Model =
    { jwtData : String
    , jwtExpires : Date.Date
    , userName : String
    , password : String
    , isLoading : Bool
    , errorMessage : Maybe String
    }


type alias LoginResponse =
    { jwtData : String
    , jwtExpires : String
    }


init : ( Model, Cmd Msg )
init =
    { jwtData = "", jwtExpires = Date.fromTime 0, userName = "", password = "", isLoading = False, errorMessage = Nothing }
        ! [ getSessionStorage "jwtData", getSessionStorage "jwtExpires" ]


isLogged : Model -> Task.Task String Bool
isLogged model =
    if model.jwtData == "" then
        Task.fail "not connected"
    else
        Task.map (\d -> DateCompare.is DateCompare.Before d model.jwtExpires) Date.now


jwtData : Model -> String
jwtData =
    .jwtData



-- UPDATE


type Msg
    = Login
    | Username String
    | Password String
    | ResponseMsg (Result Http.Error LoginResponse)
    | GetSessionStorage ( String, String )


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        Login ->
            let
                body =
                    Http.jsonBody <|
                        JEncode.object
                            [ ( "login", JEncode.string model.userName ), ( "password", JEncode.string model.password ) ]

                request =
                    Http.post "/api/auth/login" body decodeLoginResponse
            in
            ( { model | userName = "", password = "", isLoading = True, errorMessage = Nothing }, Http.send ResponseMsg request )

        Username userName ->
            ( { model | userName = userName }, Cmd.none )

        Password password ->
            ( { model | password = password }, Cmd.none )

        ResponseMsg (Ok response) ->
            let
                dateAttempt =
                    Date.fromString response.jwtExpires
            in
            case dateAttempt of
                Ok date ->
                    { model | jwtData = response.jwtData, jwtExpires = date, isLoading = False }
                        ! [ setSessionStorage ( "jwtData", response.jwtData ), setSessionStorage ( "jwtExpires", response.jwtExpires ) ]

                Err _ ->
                    ( { model | errorMessage = Just "Ouch ! Something went wrong.", isLoading = False }, Cmd.none )

        ResponseMsg (Err _) ->
            ( { model | errorMessage = Just "Ouch ! Something went wrong.", isLoading = False }, Cmd.none )

        GetSessionStorage ( key, value ) ->
            case key of
                "jwtData" ->
                    ( { model | jwtData = value }, Cmd.none )

                "jwtExpires" ->
                    let
                        dateAttempt =
                            Date.fromString value
                    in
                    case dateAttempt of
                        Ok date ->
                            ( { model | jwtExpires = date }, Cmd.none )

                        Err _ ->
                            ( model, Cmd.none )

                _ ->
                    ( model, Cmd.none )


decodeLoginResponse : Decoder LoginResponse
decodeLoginResponse =
    map2 LoginResponse (field "token" JDecode.string) (field "expires" JDecode.string)



-- SUBSCRIPTIONS


subscriptions : Model -> Sub Msg
subscriptions model =
    getSessionStorageResult GetSessionStorage



-- VIEW


loginForm : Model -> Html Msg
loginForm model =
    let
        buttonClass =
            if model.isLoading then
                "button is-link is-loading"
            else
                "button is-link"
    in
    section [ attribute "class" "section" ]
        [ div [ attribute "class" "container", style [ ( "maxWidth", "25rem" ) ] ]
            [ h1 [ attribute "class" "title" ] [ text "Please log-in" ]
            , form [ onSubmit Login ]
                [ div [ attribute "class" "field" ]
                    [ div [ attribute "class" "control" ]
                        [ input [ type_ "text", attribute "class" "input", onInput Username ] []
                        ]
                    ]
                , div [ attribute "class" "field" ]
                    [ div [ attribute "class" "control" ]
                        [ input [ type_ "password", attribute "class" "input", onInput Password ] []
                        ]
                    ]
                , p [ attribute "class" "help is-danger" ] [ text <| Maybe.withDefault "" model.errorMessage ]
                , div [ attribute "class" "is-grouped" ]
                    [ div [ attribute "class" "control" ]
                        [ button [ attribute "class" buttonClass ] [ text "Submit" ]
                        ]
                    ]
                ]
            ]
        ]
