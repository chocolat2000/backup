port module AppPorts exposing (..)

port setSessionStorage : ( String, String ) -> Cmd msg


port getSessionStorage : String -> Cmd msg


port getSessionStorageResult : ((String, String) -> msg) -> Sub msg

