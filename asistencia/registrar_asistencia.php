<?php
require_once 'auth.php';
require_once 'db.php';
require_once 'email_service.php';

header('Content-Type: application/json');

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    echo json_encode(["status" => "error", "message" => "Método no permitido"]);
    exit;
}

if (!verify_csrf_token($_POST['csrf_token'] ?? '')) {
    echo json_encode(["status" => "error", "message" => "Token de seguridad inválido o sesión expirada"]);
    exit;
}

$tipo = $_POST['tipo'] ?? '';
$numero_unico = trim($_POST['numero_unico'] ?? '');

if (!$tipo || !$numero_unico) {
    echo json_encode(["status" => "error", "message" => "Datos incompletos"]);
    exit;
}

$stmt = $pdo->prepare("SELECT * FROM alumnos WHERE numero_unico = ?");
$stmt->execute([$numero_unico]);
$alumno = $stmt->fetch(PDO::FETCH_ASSOC);

if (!$alumno) {
    echo json_encode(["status" => "error", "message" => "Alumno no encontrado."]);
    exit;
}

$fecha_hoy = date('Y-m-d');
$hora_ahora = date('H:i:s');

if ($tipo === 'colegio') {
    $stmt = $pdo->prepare("SELECT * FROM asistencia_colegio WHERE alumno_id = ? AND fecha = ?");
    $stmt->execute([$numero_unico, $fecha_hoy]);
    $registro = $stmt->fetch(PDO::FETCH_ASSOC);

    if (!$registro) {
        // Ingreso
        try {
            $stmt = $pdo->prepare("INSERT INTO asistencia_colegio (alumno_id, fecha, hora_ingreso) VALUES (?, ?, ?)");
            $stmt->execute([$numero_unico, $fecha_hoy, $hora_ahora]);
            notificar_ingreso($alumno['email_apoderado'], $alumno['nombre'], $hora_ahora);
            echo json_encode(["status" => "success", "message" => "Ingreso registrado: " . $alumno['nombre']]);
        } catch (PDOException $e) {
            if ($e->getCode() == 23000) {
                echo json_encode(["status" => "warning", "message" => "El ingreso ya fue registrado previamente hoy."]);
            } else {
                throw $e;
            }
        }
    } else if (empty($registro['hora_salida'])) {
        // Salida
        $stmt = $pdo->prepare("UPDATE asistencia_colegio SET hora_salida = ? WHERE id = ?");
        $stmt->execute([$hora_ahora, $registro['id']]);
        notificar_salida($alumno['email_apoderado'], $alumno['nombre'], $hora_ahora);
        echo json_encode(["status" => "success", "message" => "Salida registrada: " . $alumno['nombre']]);
    } else {
        echo json_encode(["status" => "warning", "message" => "El alumno ya registró salida hoy."]);
    }
} else if ($tipo === 'comedor') {
    $stmt = $pdo->prepare("SELECT * FROM asistencia_comedor WHERE alumno_id = ? AND fecha = ?");
    $stmt->execute([$numero_unico, $fecha_hoy]);
    $registro = $stmt->fetch(PDO::FETCH_ASSOC);

    if (!$registro) {
        try {
            $stmt = $pdo->prepare("INSERT INTO asistencia_comedor (alumno_id, fecha, hora_ingreso) VALUES (?, ?, ?)");
            $stmt->execute([$numero_unico, $fecha_hoy, $hora_ahora]);
            echo json_encode(["status" => "success", "message" => "Comedor registrado: " . $alumno['nombre']]);
        } catch (PDOException $e) {
            if ($e->getCode() == 23000) {
                echo json_encode(["status" => "warning", "message" => "Ya registró asistencia en comedor hoy."]);
            } else {
                throw $e;
            }
        }
    } else {
        echo json_encode(["status" => "warning", "message" => "Ya registró asistencia en comedor hoy."]);
    }
} else {
    echo json_encode(["status" => "error", "message" => "Tipo de asistencia inválido."]);
}
?>
