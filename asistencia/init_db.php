<?php
require_once __DIR__ . '/session_config.php';
require_once 'db.php';

// Si la tabla de usuarios ya existe y tiene registros, exigir sesión activa de administrador
try {
    $check = $pdo->query("SHOW TABLES LIKE 'usuarios'")->fetch();
    if ($check) {
        $count = $pdo->query("SELECT COUNT(*) FROM usuarios")->fetchColumn();
        if ($count > 0 && (!isset($_SESSION['admin_logged_in']) || $_SESSION['admin_logged_in'] !== true)) {
            http_response_code(403);
            die("<h1>Acceso denegado</h1><p>El sistema ya se encuentra instalado. Inicie sesión como administrador para gestionar la base de datos.</p><p><a href='index.php'>Ir al inicio de sesión</a></p>");
        }
    }
} catch (Exception $e) {
    // Si la base de datos aún no existe o está vacía, continuar con la instalación inicial
}

$sql = "
CREATE TABLE IF NOT EXISTS alumnos (
    numero_unico VARCHAR(50) PRIMARY KEY,
    nombre VARCHAR(255) NOT NULL,
    curso VARCHAR(50) NOT NULL,
    email_apoderado VARCHAR(255) NOT NULL,
    token_qr VARCHAR(100) NULL
);

CREATE TABLE IF NOT EXISTS asistencia_colegio (
    id INT AUTO_INCREMENT PRIMARY KEY,
    alumno_id VARCHAR(50),
    fecha DATE NOT NULL,
    hora_ingreso TIME NOT NULL,
    hora_salida TIME,
    UNIQUE KEY uq_colegio_dia (alumno_id, fecha),
    FOREIGN KEY (alumno_id) REFERENCES alumnos(numero_unico) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS asistencia_comedor (
    id INT AUTO_INCREMENT PRIMARY KEY,
    alumno_id VARCHAR(50),
    fecha DATE NOT NULL,
    hora_ingreso TIME NOT NULL,
    UNIQUE KEY uq_comedor_dia (alumno_id, fecha),
    FOREIGN KEY (alumno_id) REFERENCES alumnos(numero_unico) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS usuarios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    rol VARCHAR(20) NOT NULL DEFAULT 'operador'
);

CREATE TABLE IF NOT EXISTS bitacora (
    id INT AUTO_INCREMENT PRIMARY KEY,
    fecha_hora DATETIME NOT NULL,
    usuario VARCHAR(50) NOT NULL,
    accion VARCHAR(100) NOT NULL,
    detalles TEXT NULL,
    ip VARCHAR(45) NOT NULL
);
";

try {
    $pdo->exec($sql);

    // Migraciones automáticas seguras si las tablas ya existían
    try {
        $pdo->exec("ALTER TABLE usuarios ADD COLUMN rol VARCHAR(20) NOT NULL DEFAULT 'operador'");
    } catch (Exception $ignored) {}

    try {
        $pdo->exec("ALTER TABLE asistencia_colegio ADD UNIQUE KEY uq_colegio_dia (alumno_id, fecha)");
    } catch (Exception $ignored) {}

    try {
        $pdo->exec("ALTER TABLE asistencia_comedor ADD UNIQUE KEY uq_comedor_dia (alumno_id, fecha)");
    } catch (Exception $ignored) {}
    
    // Crear o actualizar usuario admin por defecto
    $stmt = $pdo->prepare("SELECT * FROM usuarios WHERE username = 'admin'");
    $stmt->execute();
    $adminUser = $stmt->fetch();
    if (!$adminUser) {
        $hash = password_hash('admin', PASSWORD_DEFAULT);
        $pdo->prepare("INSERT INTO usuarios (username, password, rol) VALUES ('admin', ?, 'admin')")->execute([$hash]);
    } else {
        $pdo->exec("UPDATE usuarios SET rol = 'admin' WHERE username = 'admin'");
    }

    // Crear usuario operador por defecto si no existe
    $stmt_op = $pdo->prepare("SELECT * FROM usuarios WHERE username = 'operador'");
    $stmt_op->execute();
    if (!$stmt_op->fetch()) {
        $hash_op = password_hash('operador123', PASSWORD_DEFAULT);
        $pdo->prepare("INSERT INTO usuarios (username, password, rol) VALUES ('operador', ?, 'operador')")->execute([$hash_op]);
    }
    
    echo "<h1>Tablas creadas exitosamente.</h1>";
    echo "<p>Se ha configurado la base de datos de manera segura.</p>";
    echo "<a href='index.php' class='btn btn-primary'>Ir al inicio</a>";
} catch (PDOException $e) {
    error_log("Error creando tablas: " . $e->getMessage());
    echo "<h1>Error en la instalación</h1>";
    echo "<p>Ocurrió un error al configurar las tablas. Revise el archivo de registro de errores (error_log) del servidor.</p>";
}
?>
