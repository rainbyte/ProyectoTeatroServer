module Queries

open Dapper
open Microsoft.Data.Sqlite

open Model

let private conn () = new SqliteConnection "Data Source = ./teatro.db;"

module Artista =
    let GetSingleById id =
        let sql = "SELECT id, nombre AS nombreYApellido FROM artista WHERE id = @id"
        let data = dict [ "id", box id ]
        conn().QuerySingle<Artista>(sql, data)
    let GetAll () =
        let sql = "SELECT id, nombre AS nombreYApellido FROM artista"
        conn().Query<Artista>(sql)

module Etiqueta =
    let GetAll () =
        let sql = "SELECT * FROM etiqueta"
        conn().Query<string>(sql)

module ActividadGeneral =
    type DAO = {
        id: int64;
        nombre: string;
        descripcion: string;
    }
    let private wrap = System.Func<_, _, _, _>(fun act fh ubi -> {
        id = act.id;
        nombre = act.nombre;
        descripcion = act.descripcion;
        ubicacion = ubi;
        fecha_hora = fh})
    let GetSingleById id =
        let sql = """
            SELECT a.id, a.nombre, a.descripcion, a.fecha, a.hora, u.*
            FROM actividad_general a
            INNER JOIN ubicacion u ON a.id_ubicacion = u.id
            WHERE a.id = @id
            LIMIT 1
        """
        let data = dict [ "id", box id ]
        conn().Query<DAO, FechaHora, Ubicacion, ActividadGeneral>(
                sql, wrap, data, splitOn="fecha,id")
            |> Seq.exactlyOne

    let GetAll () =
        let sql = """
            SELECT a.id, a.nombre, a.descripcion, a.fecha, a.hora, u.*
            FROM actividad_general a
            INNER JOIN ubicacion u ON a.id_ubicacion = u.id
        """
        conn().Query<DAO, FechaHora, Ubicacion, ActividadGeneral>(
                sql, wrap, splitOn="fecha,id")

module ActividadObra =
    type DAO = {
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
    let private wrap = System.Func<_,_,_>(fun act ubi ->
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
            artistas = conn().Query<Artista>(sqlArtistas, param) |> Seq.toArray;
            autor = act.autor;
            puntaje = act.puntaje;
            puntajePromedio = act.puntaje_promedio;
            valoracion = act.valoracion;
            valoracionPromedio = act.valoracion_promedio;
            direccion = act.direccion;
            sinopsis = act.sinopsis;
            etiquetas = conn().Query<string>(sqlEtiquetas, param) |> Seq.toArray;
            tematica = act.tematica;
            fecha_hora = conn().Query<FechaHora>(sqlFunciones, param) |> Seq.toArray;
            imagenes = conn().Query<Imagen>(sqlImagenes, param) |> Seq.toArray;
        })
    let GetAll () =
        let sql = """
            SELECT ao.*, ubi.*
            FROM actividad_obra ao
            INNER JOIN ubicacion_cap ubi ON ao.id_ubicacion_cap = ubi.id
        """
        conn().Query<DAO, UbicacionConCapacidad, ActividadObra>(
                sql, wrap)
    let GetSingleById (id: int64) =
        let sql = """
            SELECT ao.*, ubi.*
            FROM actividad_obra ao
            INNER JOIN ubicacion_cap ubi ON ao.id_ubicacion_cap = ubi.id
            WHERE ao.id = @id
            LIMIT 1
        """
        let param = dict ["id", box id]
        conn().Query<DAO, UbicacionConCapacidad, ActividadObra>(
                sql, wrap, param)
            |> Seq.exactlyOne

module Notificacion =
    let GetAll () =
        let sql = """
            SELECT id, fecha, titulo, descripcion,
                id_actividad AS idActividad,
                tipo_actividad AS tipoActividad,
                tipo_notificacion AS tipoNotificacion
            FROM notificacion
        """
        conn().Query<Notificacion>(sql)

module Usuario =
    let private genAddInteres tableSuffix field idUsuario fieldValue =
        let genSql = sprintf """
            INSERT INTO usuario_interes_%s (id_usuario, %s)
            VALUES (@id_usuario, @value);
        """
        let param = dict [
            "id_usuario", box idUsuario;
            "value", box fieldValue]
        conn().Query(genSql tableSuffix field, param) |> ignore
    let private genDelInteres tableSuffix field idUsuario fieldValue =
        let genSql = sprintf """
            DELETE FROM usuario_interes_%s
            WHERE id_usuario = @id_usuario
              AND %s = @value;
        """
        let param = dict [
            "id_usuario", box idUsuario;
            "value", box fieldValue]
        conn().Query(genSql tableSuffix field, param) |> ignore
    let AddInteresArtista : string -> int64 -> unit =
        genAddInteres "artista" "id_artista"
    let AddInteresEtiqueta : string -> string -> unit =
        genAddInteres "etiqueta" "etiqueta"
    let AddInteresObra (idUsuario: string) (idObra: int64) =
        genAddInteres "obra" "id_obra" idUsuario idObra
    let DelInteresArtista : string -> int64 -> unit =
        genDelInteres "artista" "id_artista"
    let DelInteresEtiqueta : string -> string -> unit =
        genDelInteres "etiqueta" "etiqueta"
    let DelInteresObra (idUsuario: string) (idObra: int64) =
        genDelInteres "obra" "id_obra" idUsuario idObra

    let private genGetAllInteres<'t> tableSuffix field idUsuario =
        let genSql = sprintf """
            SELECT %s FROM usuario_interes_%s
            WHERE id_usuario = @id_usuario
        """
        let param = dict [ "id_usuario", box idUsuario ]
        conn().Query<'t>(genSql field tableSuffix, param) |> Seq.toArray
    let GetAllInteresArtista : string -> int64 [] =
        genGetAllInteres<int64> "artista" "id_artista"
    let GetAllInteresEtiqueta : string -> string [] =
        genGetAllInteres<string> "etiqueta" "etiqueta"
    let GetAllInteresObra : string -> int64 [] =
        genGetAllInteres<int64> "obra" "id_obra"