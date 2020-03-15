module Model

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
