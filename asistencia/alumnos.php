<?php 
$require_admin = true;
require_once 'auth.php';
require_once 'db.php';
require_once 'audit.php';
$msg = '';
$error = '';

function validaRut($rut) {
    // Remover puntos, comas y espacios
    $rut = str_replace(array('.', ',', ' '), '', trim($rut));
    
    // Si no tiene guion, se lo agregamos antes del último dígito
    if (strpos($rut, '-') === false && strlen($rut) > 1) {
        $rut = substr($rut, 0, -1) . '-' . substr($rut, -1);
    }
    
    if (!preg_match("/^[0-9]+-[0-9kK]{1}$/", $rut)) return false;
    
    $rutParts = explode('-', strtolower($rut));
    $numero = $rutParts[0];
    $dv = $rutParts[1];
    
    $i = 2; $suma = 0;
    foreach(array_reverse(str_split($numero)) as $v) {
        if($i == 8) $i = 2;
        $suma += $v * $i;
        ++$i;
    }
    $dvr = 11 - ($suma % 11);
    if($dvr == 11) $dvr = 0;
    if($dvr == 10) $dvr = 'k';
    
    return (string)$dvr === $dv;
}

// Procesar Eliminar
if (isset($_POST['eliminar_id'])) {
    if (!verify_csrf_token($_POST['csrf_token'] ?? '')) {
        $error = "Token de seguridad inválido.";
    } else {
        $id = $_POST['eliminar_id'];
        $stmt = $pdo->prepare("DELETE FROM alumnos WHERE numero_unico = ?");
        $stmt->execute([$id]);
        registrar_bitacora($pdo, 'ELIMINAR_ALUMNO', "RUT: {$id}");
        $msg = "Alumno eliminado.";
    }
}

// Procesar Nuevo Alumno Manual
if (isset($_POST['nuevo_alumno'])) {
    if (!verify_csrf_token($_POST['csrf_token'] ?? '')) {
        $error = "Token de seguridad inválido.";
    } else {
        $numero = trim($_POST['numero_unico'] ?? '');
        $nombre = trim($_POST['nombre'] ?? '');
        $curso = trim($_POST['curso'] ?? '') ?: 'Sin Curso';
        $email = trim($_POST['email_apoderado'] ?? '');
        
        // Normalizar RUT antes de guardar
        $numero = str_replace(array('.', ',', ' '), '', $numero);
        if (strpos($numero, '-') === false && strlen($numero) > 1) {
            $numero = substr($numero, 0, -1) . '-' . substr($numero, -1);
        }
        $numero = strtoupper($numero);

        if (!validaRut($numero)) {
            $error = "El RUT ingresado no es válido.";
        } else {
            $stmt = $pdo->prepare("SELECT numero_unico FROM alumnos WHERE numero_unico = ?");
            $stmt->execute([$numero]);
            if ($stmt->fetch()) {
                $error = "El RUT/ID ya existe.";
            } else {
                $stmt = $pdo->prepare("INSERT INTO alumnos (numero_unico, nombre, curso, email_apoderado) VALUES (?, ?, ?, ?)");
                $stmt->execute([$numero, $nombre, $curso, $email]);
                registrar_bitacora($pdo, 'CREAR_ALUMNO_MANUAL', "RUT: {$numero}, Nombre: {$nombre}");
                $msg = "Alumno " . htmlspecialchars($nombre, ENT_QUOTES, 'UTF-8') . " agregado exitosamente.";
            }
        }
    }
}

// Procesar Carga Masiva (CSV) con Transacciones y Límite de Filas
if (isset($_FILES['file_csv'])) {
    if (!verify_csrf_token($_POST['csrf_token'] ?? '')) {
        $error = "Token de seguridad inválido.";
    } elseif ($_FILES['file_csv']['size'] > 5 * 1024 * 1024) { // Máximo 5MB
        $error = "El archivo es demasiado grande (máximo 5MB).";
    } else {
        $file = $_FILES['file_csv']['tmp_name'];
        if (is_uploaded_file($file)) {
            $handle = fopen($file, "r");
            
            // Detectar delimitador leyendo la primera línea
            $firstLine = fgets($handle);
            $delimiter = (strpos($firstLine, ';') !== false) ? ';' : ',';
            rewind($handle); // Volver al inicio del archivo

            $agregados = 0;
            $errores = 0;
            $row = 0;
            $max_filas = 1500;
            $limite_excedido = false;
            
            // Iniciar transacción para garantizar atomicidad e integridad
            $pdo->beginTransaction();

            try {
                $stmt_check = $pdo->prepare("SELECT numero_unico FROM alumnos WHERE numero_unico = ?");
                $stmt_insert = $pdo->prepare("INSERT INTO alumnos (numero_unico, nombre, curso, email_apoderado) VALUES (?, ?, ?, ?)");

                while (($data = fgetcsv($handle, 1000, $delimiter)) !== FALSE) {
                    $row++;
                    if ($row == 1) continue; // Saltar cabecera
                    
                    if ($row > $max_filas) {
                        $limite_excedido = true;
                        break;
                    }
                    
                    if (count($data) >= 2) {
                        $numero = preg_replace('/[\x00-\x1F\x7F\xA0]/u', '', trim($data[0]));
                        $nombre = trim($data[1]);
                        $curso = isset($data[2]) ? trim($data[2]) : 'Sin Curso';
                        $email = isset($data[3]) ? trim($data[3]) : '';
                        
                        // Mitigar inyección de fórmulas CSV
                        if (in_array(substr($nombre, 0, 1), ['=', '+', '-', '@', "\t", "\r"])) {
                            $nombre = "'" . $nombre;
                        }
                        if (in_array(substr($curso, 0, 1), ['=', '+', '-', '@', "\t", "\r"])) {
                            $curso = "'" . $curso;
                        }

                        if ($numero && $nombre) {
                            if (!validaRut($numero)) {
                                $errores++;
                                continue;
                            }
                            
                            $numero = str_replace(array('.', ',', ' '), '', $numero);
                            if (strpos($numero, '-') === false) {
                                $numero = substr($numero, 0, -1) . '-' . substr($numero, -1);
                            }
                            $numero = strtoupper($numero);

                            if ($email && !filter_var($email, FILTER_VALIDATE_EMAIL)) {
                                $email = '';
                            }

                            $stmt_check->execute([$numero]);
                            if (!$stmt_check->fetch()) {
                                $stmt_insert->execute([$numero, $nombre, $curso, $email]);
                                $agregados++;
                            }
                        }
                    }
                }
                fclose($handle);

                if ($limite_excedido) {
                    $pdo->rollBack();
                    $error = "El archivo supera el límite seguro de {$max_filas} registros por carga. Operación cancelada por seguridad.";
                } else {
                    $pdo->commit();
                    registrar_bitacora($pdo, 'CARGA_MASIVA_CSV', "Agregados: {$agregados}, Omitidos/Errores: {$errores}");
                    $msg = "Se agregaron {$agregados} alumnos exitosamente.";
                    if ($errores > 0) {
                        $msg .= " ({$errores} fila(s) omitida(s) por RUT inválido).";
                    }
                }
            } catch (Exception $e) {
                if ($pdo->inTransaction()) {
                    $pdo->rollBack();
                }
                error_log("Error en carga masiva: " . $e->getMessage());
                $error = "Error al procesar la carga masiva en la base de datos.";
            }
        } else {
            $error = "Error al subir el archivo.";
        }
    }
}

$stmt = $pdo->query("SELECT * FROM alumnos");
$alumnos = $stmt->fetchAll(PDO::FETCH_ASSOC);

include 'header.php'; 
?>

<div class="row mb-4">
    <div class="col-md-6">
        <h2>Gestión de Alumnos</h2>
    </div>
    <div class="col-md-6 text-end">
        <button class="btn btn-primary me-2" data-bs-toggle="modal" data-bs-target="#manualModal">Agregar Manualmente</button>
        <button class="btn btn-success" data-bs-toggle="modal" data-bs-target="#uploadModal">Carga Masiva (CSV)</button>
    </div>
</div>

<?php if($msg): ?><div class="alert alert-success"><?php echo htmlspecialchars($msg); ?></div><?php endif; ?>
<?php if($error): ?><div class="alert alert-danger"><?php echo htmlspecialchars($error); ?></div><?php endif; ?>

<div class="row">
    <div class="col-md-12">
        <table class="table table-striped table-bordered">
            <thead class="table-dark">
                <tr>
                    <th>ID / RUT</th>
                    <th>Nombre</th>
                    <th>Curso</th>
                    <th>Email Apoderado</th>
                    <th>Acciones</th>
                </tr>
            </thead>
            <tbody>
                <?php foreach($alumnos as $a): ?>
                <tr>
                    <td><?php echo htmlspecialchars($a['numero_unico']); ?></td>
                    <td><?php echo htmlspecialchars($a['nombre']); ?></td>
                    <td><?php echo htmlspecialchars($a['curso']); ?></td>
                    <td><?php echo htmlspecialchars($a['email_apoderado']); ?></td>
                    <td>
                        <button type="button" class="btn btn-sm btn-info text-white btn-ver-barcode" 
                                data-id="<?php echo htmlspecialchars($a['numero_unico'], ENT_QUOTES, 'UTF-8'); ?>" 
                                data-nombre="<?php echo htmlspecialchars($a['nombre'], ENT_QUOTES, 'UTF-8'); ?>">Ver Código de Barras</button>
                        <form method="post" action="alumnos.php" class="d-inline" onsubmit="return confirm('¿Seguro que deseas eliminar este alumno?');">
                            <?php csrf_field(); ?>
                            <input type="hidden" name="eliminar_id" value="<?php echo htmlspecialchars($a['numero_unico'], ENT_QUOTES, 'UTF-8'); ?>">
                            <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                        </form>
                    </td>
                </tr>
                <?php endforeach; ?>
                <?php if(empty($alumnos)): ?>
                <tr><td colspan="5" class="text-center">No hay alumnos registrados.</td></tr>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Modals -->
<div class="modal fade" id="uploadModal" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog">
    <div class="modal-content">
      <form action="alumnos.php" method="post" enctype="multipart/form-data">
          <?php csrf_field(); ?>
          <div class="modal-header">
            <h5 class="modal-title">Cargar CSV de Alumnos</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body">
            <p>Sube un archivo <strong>CSV</strong> delimitado por comas con las columnas: RUT, Nombre, Curso, Email.</p>
            <input type="file" class="form-control" name="file_csv" accept=".csv" required>
          </div>
          <div class="modal-footer">
            <button type="submit" class="btn btn-success">Subir CSV</button>
          </div>
      </form>
    </div>
  </div>
</div>

<div class="modal fade" id="manualModal" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog">
    <div class="modal-content">
      <form action="alumnos.php" method="post">
          <?php csrf_field(); ?>
          <div class="modal-header">
            <h5 class="modal-title">Agregar Alumno</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body">
            <input type="hidden" name="nuevo_alumno" value="1">
            <div class="mb-3"><label>RUT / ID</label><input type="text" class="form-control" name="numero_unico" required></div>
            <div class="mb-3"><label>Nombre</label><input type="text" class="form-control" name="nombre" required></div>
            <div class="mb-3"><label>Curso</label><input type="text" class="form-control" name="curso"></div>
            <div class="mb-3"><label>Email Apoderado</label><input type="email" class="form-control" name="email_apoderado"></div>
          </div>
          <div class="modal-footer">
            <button type="submit" class="btn btn-primary">Guardar</button>
          </div>
      </form>
    </div>
  </div>
</div>

<!-- Modal Código de Barras -->
<div class="modal fade" id="barcodeModal" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog text-center">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Código de Barras del Alumno</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <p class="fw-bold fs-5" id="barcodeAlumnoNombre"></p>
        <p class="text-muted small">Listo para lectura con pistola USB en portería y comedor.</p>
        <div class="p-3 bg-white border rounded">
            <svg id="barcodeImage"></svg>
        </div>
      </div>
    </div>
  </div>
</div>

<!-- Librería JS para generar Código de Barras en el navegador -->
<script src="https://cdn.jsdelivr.net/npm/jsbarcode@3.11.0/dist/JsBarcode.all.min.js"></script>
<script>
    function mostrarBarcode(numero_unico, nombre) {
        document.getElementById('barcodeAlumnoNombre').innerText = nombre;
        JsBarcode("#barcodeImage", numero_unico, { format: "CODE128", width: 2, height: 60, displayValue: true });
        
        var modal = new bootstrap.Modal(document.getElementById('barcodeModal'));
        modal.show();
    }

    document.addEventListener('DOMContentLoaded', function() {
        document.querySelectorAll('.btn-ver-barcode').forEach(function(btn) {
            btn.addEventListener('click', function() {
                mostrarBarcode(this.dataset.id, this.dataset.nombre);
            });
        });
    });
</script>

<?php include 'footer.php'; ?>
