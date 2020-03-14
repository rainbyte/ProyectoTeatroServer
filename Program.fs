module Program

open Dapper
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Data.Sqlite
open Microsoft.IdentityModel.Tokens
open Saturn
open System.Security.Claims

type Artista = {
    id: int64;
    nombreYApellido: string;
}

type Notificacion = {
    id: int64;
    fecha: string;
    titulo: string;
    descripcion: string;
    idActividad: int64;
    tipoActividad: string; // generales u obra
    tipoNotificacion: string; // generales
}

type Ubicacion = {
    id: int64;
    latitud: float;
    longitud: float;
    descripcion: string;
}

type UbicacionConCapacidad = {
    id: int64;
    latitud: float;
    longitud: float;
    descripcion: string;
    capacidad: int64;
}

type FechaHora = {
    fecha: string;
    hora: string;
}

type Imagen = {
    url: string;
}

type ActividadGeneral = {
    id: int64;
    nombre: string;
    descripcion: string;
    ubicacion: Ubicacion;
    fecha_hora: FechaHora;
}

type ActividadObra = {
    id: int64;
    nombre: string;
    descripcion: string;
    ubicacion: UbicacionConCapacidad;
    elenco: string;
    artistas: array<Artista>;
    autor: string;
    puntaje: int64;
    puntajePromedio: float;
    valoracion: int64;
    valoracionPromedio: float;
    direccion: string;
    sinopsis: string;
    etiquetas: array<string>;
    tematica: string;
    fecha_hora: array<FechaHora>;
    imagenes: array<Imagen>;
}

type Actividades = {
    generales: array<ActividadGeneral>;
    obras: array<ActividadObra>;
}


module Queries =
    module Artista =
        let GetSingleById id =
            let db = new SqliteConnection "Data Source = ./teatro.db;" 
            let sql = "SELECT id, nombre AS nombreYApellido FROM artista WHERE id = @id"
            let data = dict [ "id", box id ]
            db.QuerySingle<Artista>(sql, data)
        let GetAll () =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = "SELECT id, nombre AS nombreYApellido FROM artista"
            db.Query<Artista>(sql)
    module Etiqueta =
        let GetAll () =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = "SELECT * FROM etiqueta"
            db.Query<string>(sql)
    module ActividadGeneral =
        type Raw = {
            id: int64;
            nombre: string;
            descripcion: string;
            id_ubicacion: int64;
            fecha: string;
            hora: string;
        }
        let wrap act ubi = {
            id = act.id;
            nombre = act.nombre;
            descripcion = act.descripcion;
            ubicacion = ubi;
            fecha_hora = {
                fecha = act.fecha;
                hora = act.hora;
            }}
        let GetSingleById id =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = """
                SELECT a.*, u.*
                FROM actividad_general a
                INNER JOIN ubicacion u ON a.id_ubicacion = u.id
                WHERE a.id = @id
                LIMIT 1
            """
            let data = dict [ "id", box id ]
            db.Query<Raw, Ubicacion, ActividadGeneral>(sql, (fun act ubi -> wrap act ubi), data)
                |> Seq.exactlyOne

        let GetAll () =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = """
                SELECT a.*, u.*
                FROM actividad_general a
                INNER JOIN ubicacion u ON a.id_ubicacion = u.id
            """
            db.Query<Raw, Ubicacion, ActividadGeneral>(sql, fun act ubi -> wrap act ubi)
    module ActividadObra =
        type Raw = {
            id: int64;
            nombre: string;
            descripcion: string;
            id_ubicacion_cap: int64;
            elenco: string;
            autor: string;
            puntaje: int64;
            puntaje_promedio: float;
            valoracion: int64;
            valoracion_promedio: float;
            direccion: string;
            sinopsis: string;
            tematica: string;
        }
        let private wrap act ubi =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sqlArtistas = """
                SELECT id, nombre as nombreYApellido
                FROM artista art
                INNER JOIN actividad_obra_artistas r ON r.id_artista = art.id
                WHERE r.id_obra = @id_obra
            """
            let sqlEtiquetas = """
                SELECT id_etiqueta FROM actividad_obra_etiquetas
                WHERE id_obra = @id_obra
            """
            let sqlFunciones = """
                SELECT fecha, hora FROM actividad_obra_funciones
                WHERE id_obra = @id_obra
            """
            let sqlImagenes = """
                SELECT url FROM actividad_obra_imagenes
                WHERE id_obra = @id_obra
            """
            let param = dict ["id_obra", box act.id]
            {
                id = act.id;
                nombre = act.nombre;
                descripcion = act.descripcion;
                ubicacion = ubi;
                elenco = act.elenco;
                artistas = db.Query<Artista>(sqlArtistas, param) |> Seq.toArray;
                autor = act.autor;
                puntaje = act.puntaje;
                puntajePromedio = act.puntaje_promedio;
                valoracion = act.valoracion;
                valoracionPromedio = act.valoracion_promedio;
                direccion = act.direccion;
                sinopsis = act.sinopsis;
                etiquetas = db.Query<string>(sqlEtiquetas, param) |> Seq.toArray;
                tematica = act.tematica;
                fecha_hora = db.Query<FechaHora>(sqlFunciones, param) |> Seq.toArray;
                imagenes = db.Query<Imagen>(sqlImagenes, param) |> Seq.toArray;
            }
        let GetAll () =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = """
                SELECT ao.*, ubi.*
                FROM actividad_obra ao
                INNER JOIN ubicacion_cap ubi ON ao.id_ubicacion_cap = ubi.id
            """
            db.Query<Raw, UbicacionConCapacidad, ActividadObra>(sql, fun act ubi -> wrap act ubi)
        let GetSingleById (id: int64) =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = """
                SELECT ao.*, ubi.*
                FROM actividad_obra ao
                INNER JOIN ubicacion_cap ubi ON ao.id_ubicacion_cap = ubi.id
                WHERE ao.id = @id
                LIMIT 1
            """
            let param = dict ["id", box id]
            db.Query<Raw, UbicacionConCapacidad, ActividadObra>(
                    sql, (fun act ubi -> wrap act ubi), param)
                |> Seq.exactlyOne
    module Notificacion =
        let GetAll () =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = """
                SELECT id, fecha, titulo, descripcion,
                    id_actividad AS idActividad,
                    tipo_actividad AS tipoActividad,
                    tipo_notificacion AS tipoNotificacion
                FROM notificacion
            """
            db.Query<Notificacion>(sql)
    module Usuario =
        let AddInteresArtista idUsuario idArtista =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = """
                INSERT INTO usuario_interes_artista (id_usuario, id_artista)
                VALUES (@id_usuario, @id_artista);
            """
            let param = dict [
                "id_usuario", box idUsuario;
                "id_artista", box idArtista]
            db.Query(sql, param) |> ignore
        let AddInteresEtiqueta idUsuario etiqueta =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = """
                INSERT INTO usuario_interes_etiqueta (id_usuario, id_etiqueta)
                VALUES (@id_usuario, @id_etiqueta);
            """
            let param = dict [
                "id_usuario", box idUsuario;
                "id_etiqueta", box etiqueta]
            db.Query(sql, param) |> ignore
        let AddInteresObra (idUsuario: string) (idObra: int64) =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = """
                INSERT INTO usuario_interes_obra (id_usuario, id_obra)
                VALUES (@id_usuario, @id_obra);
            """
            let param = dict [
                "id_usuario", box idUsuario;
                "id_obra", box idObra]
            db.Query(sql, param) |> ignore
        let DelInteresObra (idUsuario: string) (idObra: int64) =
            let db = new SqliteConnection "Data Source = ./teatro.db;"
            let sql = """
                DELETE FROM usuario_interes_obra
                WHERE id_usuario = @id_usuario
                  AND id_obra = @id_obra;
            """
            let param = dict [
                "id_usuario", box idUsuario;
                "id_obra", box idObra]
            db.Query(sql, param) |> ignore

let onlyLoggedIn = pipeline {
    requires_authentication (Giraffe.Auth.challenge JwtBearerDefaults.AuthenticationScheme)
}

let ctrActividades = controller {
    index (fun ctx ->
        let actividades = {
            generales = Queries.ActividadGeneral.GetAll() |> Seq.toArray;
            obras = Queries.ActividadObra.GetAll() |> Seq.toArray;
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
    index (fun ctx -> Queries.ActividadObra.GetAll() |> Controller.json ctx)
    show (fun ctx (id: int64) ->
        Queries.ActividadObra.GetSingleById id |> Controller.json ctx
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
}

let routerActividades = router {
    forward "" ctrActividades
    forward "/generales" ctrActividadesGenerales
    forward "/obras" ctrActividadesObras
}

let ctrNotificaciones = controller {
    index (fun ctx -> Queries.Notificacion.GetAll() |> Controller.json ctx)
}

let ctrArtistas = controller {
    index (fun ctx -> Queries.Artista.GetAll () |> Controller.json ctx)
}

let ctrEtiquetas = controller {
    index (fun ctx -> Queries.Etiqueta.GetAll () |> Controller.json ctx)
}

let routerMain = router {
    forward "/actividades" routerActividades
    forward "/notificaciones" ctrNotificaciones
    forward "/artistas" ctrArtistas
    forward "/etiquetas" ctrEtiquetas
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