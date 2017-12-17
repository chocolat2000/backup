module Requests exposing (..)

import Http exposing (Error, emptyBody, expectJson, header, request, send)
import Json.Decode exposing (Decoder)


get : (Result Error a -> msg) -> String -> String -> Decoder a -> Cmd msg
get msg url auth decoder =
    let
        req =
            request
                { method = "GET"
                , headers = [ header "Authorization" ("Bearer " ++ auth) ]
                , url = url
                , body = emptyBody
                , expect = expectJson decoder
                , timeout = Nothing
                , withCredentials = False
                }
    in
    send msg req
