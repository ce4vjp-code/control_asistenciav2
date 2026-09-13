<?php
$require_admin = true;
require_once 'auth.php';
require_once 'db.php';
require_once 'email_service.php';
require_once 'audit.php';

$fecha = date('Y-m-d');

// Alumnos que fueron al colegio
$stmt = $pdo->prepare("SELECT a.* FROM asistencia_colegio ac JOIN alumnos a ON ac.alumno_id = a.numero_unico WHERE ac.fecha = ?");
$stmt->execute([$fecha]);
$colegio = $stmt->fetchAll(PDO::FETCH_ASSOC);

// Alumnos que fueron al comedor
$stmt = $pdo->prepare("SELECT alumno_id FROM asistencia_comedor WHERE fecha = ?");
$stmt->execute([$fecha]);
$comedor_ids = $stmt->fetchAll(PDO::FETCH_COLUMN);

$faltantes = [];
foreach($colegio as $al) {
    if(!in_array($al['numero_unico'], $comedor_ids)) {
        $faltantes[] = $al;
    }
}

// Envío protegido mediante POST y CSRF
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['enviar_alertas'])) {
    if (!verify_csrf_token($_POST['csrf_token'] ?? '')) {
        die("Acceso denegado: Token de seguridad inválido.");
    }

    try {
        notificar_alerta_comedor($faltantes);
        registrar_bitacora($pdo, 'ENVIO_ALERTA_COMEDOR', "Total alumnos reportados faltantes: " . count($faltantes));
    } catch (Throwable $e) {
        error_log("Error al enviar alerta: " . $e->getMessage());
    }
    
    if (!headers_sent()) {
        header("Location: alertas.php?enviado=1");
    } else {
        echo '<script>window.location.href="alertas.php?enviado=1";</script>';
    }
    exit;
}
?>
