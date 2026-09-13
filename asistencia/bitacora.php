<?php
$require_admin = true;
require_once 'auth.php';
require_once 'db.php';

// Obtener los últimos 100 registros de la bitácora
$stmt = $pdo->query("SELECT * FROM bitacora ORDER BY id DESC LIMIT 100");
$logs = $stmt->fetchAll(PDO::FETCH_ASSOC);

include 'header.php';
?>
<div class="row mb-4">
    <div class="col-md-6">
        <h2>Bitácora de Auditoría y Seguridad</h2>
        <p class="text-muted">Registro de actividad y eventos relevantes del sistema (Últimos 100 eventos).</p>
    </div>
</div>

<div class="row">
    <div class="col-12">
        <div class="card shadow-sm">
            <div class="card-body p-0">
                <table class="table table-striped table-hover mb-0">
                    <thead class="table-dark">
                        <tr>
                            <th>Fecha y Hora</th>
                            <th>Usuario</th>
                            <th>Acción</th>
                            <th>Detalles</th>
                            <th>Dirección IP</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($logs as $log): ?>
                        <tr>
                            <td><?php echo htmlspecialchars($log['fecha_hora']); ?></td>
                            <td><span class="badge bg-secondary"><?php echo htmlspecialchars($log['usuario']); ?></span></td>
                            <td>
                                <?php 
                                $badgeClass = 'bg-info text-dark';
                                if (strpos($log['accion'], 'FALLIDO') !== false || strpos($log['accion'], 'ELIMINAR') !== false) {
                                    $badgeClass = 'bg-danger text-white';
                                } elseif (strpos($log['accion'], 'EXITOSO') !== false || strpos($log['accion'], 'CREAR') !== false) {
                                    $badgeClass = 'bg-success text-white';
                                }
                                ?>
                                <span class="badge <?php echo $badgeClass; ?>"><?php echo htmlspecialchars($log['accion']); ?></span>
                            </td>
                            <td><?php echo htmlspecialchars($log['detalles'] ?? '-'); ?></td>
                            <td><code><?php echo htmlspecialchars($log['ip']); ?></code></td>
                        </tr>
                        <?php endforeach; ?>
                        <?php if (empty($logs)): ?>
                        <tr>
                            <td colspan="5" class="text-center py-4 text-muted">No hay registros en la bitácora de auditoría aún.</td>
                        </tr>
                        <?php endif; ?>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</div>

<?php include 'footer.php'; ?>
