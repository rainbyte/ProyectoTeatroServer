module Program

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open Saturn
open System.Security.Claims

open Model
open Queries

let onlyLoggedIn = pipeline {
    requires_authentication (Giraffe.Auth.challenge JwtBearerDefaults.AuthenticationScheme)
}

let ctrActividades = controller {
    index (fun ctx ->
        let actividades = {
            generales = Queries.ActividadGeneral.GetAll() |> Seq.toArray;
            obras = Queries.ActividadObra.GetAll "" |> Seq.toArray;
        }
        actividades |> Controller.json ctx)
}

let ctrActividadesGenerales = controller {
    index (fun ctx -> Queries.ActividadGeneral.GetAll () |> Controller.json ctx)
    show (fun ctx (id: int64) ->
        Queries.ActividadGeneral.GetSingleById id |> Controller.json ctx
    )
}

let ctrActividadesObras = controller {
    index (fun ctx -> Queries.ActividadObra.GetAll "" |> Controller.json ctx)
    show (fun ctx (id: int64) ->
        Queries.ActividadObra.GetSingleById "" id |> Controller.json ctx
    )

    subController "/meInteresa" (fun obraId -> router {
        pipe_through onlyLoggedIn
        put "" (fun next ctx -> task {
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier
            let! hasInteres = Controller.getJson<bool> ctx
            if hasInteres
                then Queries.Usuario.AddInteresObra username.Value obraId
                else Queries.Usuario.DelInteresObra username.Value obraId
            return! next ctx})
    })

    subController "/valorar" (fun obraId -> router {
        pipe_through onlyLoggedIn
        putf "/%i" (fun puntos next ctx -> task {
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier
            printfn "puntuacion=%A" puntos
            if puntos >= 0 && puntos <= 10
                then Queries.Usuario.AddPuntuacionObra username.Value obraId (int64 puntos)
            return! next ctx})
    })
}

let routerActividades = router {
    forward "" ctrActividades
    forward "/generales" ctrActividadesGenerales
    forward "/obras" ctrActividadesObras
    forward "/obras/meInteresa" (router {
        pipe_through onlyLoggedIn
        getf "/%s" (fun usernameUnchecked next ctx -> task {
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier
            if usernameUnchecked = username.Value
                then return!
                        Queries.Usuario.GetAllInteresObra username.Value
                        |> Controller.json ctx
                else return None})
    })
}

let ctrNotificaciones = controller {
    index (fun ctx -> Queries.Notificacion.GetAll() |> Controller.json ctx)
}

let ctrArtistas = controller {
    index (fun ctx -> Queries.Artista.GetAll () |> Controller.json ctx)

    subController "/meInteresa" (fun idArtista -> router {
        pipe_through onlyLoggedIn
        put "" (fun next ctx -> task {
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier
            let! hasInteres = Controller.getJson<bool> ctx
            if hasInteres
                then Queries.Usuario.AddInteresArtista username.Value idArtista
                else Queries.Usuario.DelInteresArtista username.Value idArtista
            return! next ctx})
    })
}

let ctrEtiquetas = controller {
    index (fun ctx -> Queries.Etiqueta.GetAll () |> Controller.json ctx)

    subController "/meInteresa" (fun etiqueta -> router {
        pipe_through onlyLoggedIn
        put "" (fun next ctx -> task {
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier
            let! hasInteres = Controller.getJson<bool> ctx
            if hasInteres
                then Queries.Usuario.AddInteresEtiqueta username.Value etiqueta
                else Queries.Usuario.DelInteresEtiqueta username.Value etiqueta
            return! next ctx})
    })
}

let routerMain = router {
    forward "/actividades" routerActividades
    forward "/notificaciones" ctrNotificaciones
    forward "/artistas" ctrArtistas
    forward "/artistas/meInteresa" (router {
        pipe_through onlyLoggedIn
        getf "/%s" (fun usernameUnchecked next ctx -> task {
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier
            if usernameUnchecked = username.Value
                then return!
                        Queries.Usuario.GetAllInteresArtista username.Value
                        |> Controller.json ctx
                else return None})
    })
    forward "/etiquetas" ctrEtiquetas
    forward "/etiquetas/meInteresa" (router {
        pipe_through onlyLoggedIn
        getf "/%s" (fun usernameUnchecked next ctx -> task {
            let username = ctx.User.FindFirst ClaimTypes.NameIdentifier
            if usernameUnchecked = username.Value
                then return!
                        Queries.Usuario.GetAllInteresEtiqueta username.Value
                        |> Controller.json ctx
                else return None})
    })
}

let app = application {
    use_jwt_authentication_with_config (fun (opts: JwtBearerOptions) ->
        opts.Authority <- "https://securetoken.google.com/labogrupo2-f386a"
        opts.TokenValidationParameters <- TokenValidationParameters(
            ValidateIssuer = true,
            ValidIssuer = "https://securetoken.google.com/labogrupo2-f386a",
            ValidateAudience = true,
            ValidAudience = "labogrupo2-f386a",
            ValidateLifetime = true)
        ())
    use_router routerMain
    url "http://0.0.0.0:5000"
    use_cors "CORS" (fun builder -> builder.WithOrigins("*").AllowAnyMethod().WithHeaders("content-type") |> ignore)
    service_config (fun s -> s.AddGiraffe())
}

[<EntryPoint>]
let main _ =
    run app
    0 // exit code