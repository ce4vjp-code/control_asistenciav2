<?php
// Prevenir acceso directo al archivo
if (basename($_SERVER['PHP_SELF']) === 'audit.php') {
    http_response_code(403);
    exit('Acceso denegado');
}

/**
 * Registra un evento en la tabla de bitácora para auditoría forense
 * 
 * @param PDO $pdo Instancia de conexión a base de datos
 * @param string $accion Nombre corto de la acción (ej: 'LOGIN_EXITOSO', 'ELIMINAR_ALUMNO')
 * @param string $detalles Descripción adicional o identificadores
 */
function registrar_bitacora($pdo, $accion, $detalles = '') {
    try {
        $usuario = $_SESSION['admin_username'] ?? 'invitado';
        $ip = $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1';
        $stmt = $pdo->prepare("INSERT INTO bitacora (fecha_hora, usuario, accion, detalles, ip) VALUES (NOW(), ?, ?, ?, ?)");
        $stmt->execute([$usuario, $accion, $detalles, $ip]);
    } catch (Exception $e) {
        // En caso de que la tabla bitácora aún no se haya creado, registrar en log de PHP
        error_log("Fallo al escribir en bitácora [{$accion}]: " . $e->getMessage());
    }
}
