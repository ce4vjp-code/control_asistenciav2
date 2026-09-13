CREATE TABLE IF NOT EXISTS Alumnos (
    NumeroUnico VARCHAR(20) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Curso VARCHAR(50) NOT NULL,
    EmailApoderado VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS AsistenciaColegio (
    AlumnoId VARCHAR(20),
    Fecha DATE,
    HoraIngreso TIME NOT NULL,
    HoraSalida TIME,
    PRIMARY KEY (AlumnoId, Fecha)
);

CREATE TABLE IF NOT EXISTS AsistenciaComedor (
    AlumnoId VARCHAR(20),
    Fecha DATE,
    HoraIngreso TIME NOT NULL,
    PRIMARY KEY (AlumnoId, Fecha)
);

CREATE TABLE IF NOT EXISTS Usuarios (
    Username VARCHAR(50) PRIMARY KEY,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(50) NOT NULL,
    Email VARCHAR(100)
);

INSERT IGNORE INTO Usuarios (Username, PasswordHash, Role, Email) 
VALUES ('cirdam', 'hVlA3bL35G4Tz3jK7B3hVQ+YVqL3M4/2s6Y8bQ==', 'admin', 'notificaciones@liceotpggm.cl');
