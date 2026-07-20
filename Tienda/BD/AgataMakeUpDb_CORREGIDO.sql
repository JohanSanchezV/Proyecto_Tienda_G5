USE [master];
GO

CREATE DATABASE AgataMakeUpDB;
GO

USE AgataMakeUpDB;
GO

CREATE TABLE Usuario (
    id_usuario INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    apellido VARCHAR(100) NOT NULL,
    correo VARCHAR(150) NOT NULL UNIQUE,
    telefono VARCHAR(20),
    contrasena VARCHAR(255) NOT NULL,
    tipo_usuario VARCHAR(20) NOT NULL DEFAULT 'CLIENTE',
    preferencias VARCHAR(MAX),
    puntos_acumulados INT DEFAULT 0,
    fecha_registro DATETIME DEFAULT GETDATE(),
    fecha_actualizacion DATETIME,
    tiene_contrasena_temporal BIT NOT NULL DEFAULT 0,
    vigencia_contrasena_temporal DATETIME NULL,
    estado BIT DEFAULT 1
);
GO

CREATE TABLE Direccion (
    id_direccion INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL UNIQUE,
    provincia VARCHAR(100) NOT NULL,
    canton VARCHAR(100) NOT NULL,
    distrito VARCHAR(100) NOT NULL,
    detalles VARCHAR(255),

    CONSTRAINT FK_Direccion_Usuario
        FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario)
);
GO

CREATE TABLE Categoria (
    id_categoria INT IDENTITY(1,1) PRIMARY KEY,
    nombre_categoria VARCHAR(100) NOT NULL,
    descripcion VARCHAR(MAX),
    estado BIT DEFAULT 1
);
GO

CREATE TABLE Marca (
    id_marca INT IDENTITY(1,1) PRIMARY KEY,
    nombre_marca VARCHAR(100) NOT NULL,
    descripcion VARCHAR(MAX),
    estado BIT DEFAULT 1
);
GO

CREATE TABLE Producto (
    id_producto INT IDENTITY(1,1) PRIMARY KEY,
    id_categoria INT NOT NULL,
    id_marca INT NULL,
    nombre_producto VARCHAR(150) NOT NULL,
    descripcion VARCHAR(MAX),
    precio DECIMAL(10,2) NOT NULL,
    stock INT NOT NULL DEFAULT 0,
    imagen_url VARCHAR(255),
    estado BIT DEFAULT 1,

    CONSTRAINT FK_Producto_Categoria
        FOREIGN KEY (id_categoria)
        REFERENCES Categoria(id_categoria),

    CONSTRAINT FK_Producto_Marca
        FOREIGN KEY (id_marca)
        REFERENCES Marca(id_marca)
);
GO

CREATE TABLE Favorito (
    id_favorito INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    id_producto INT NOT NULL,
    fecha_agregado DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Favorito_Usuario
        FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario),

    CONSTRAINT FK_Favorito_Producto
        FOREIGN KEY (id_producto)
        REFERENCES Producto(id_producto),

    CONSTRAINT UQ_Favorito_Usuario_Producto
        UNIQUE (id_usuario, id_producto)
);
GO

CREATE TABLE Carrito (
    id_carrito INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    fecha_creacion DATETIME DEFAULT GETDATE(),
    estado_carrito VARCHAR(20) DEFAULT 'Activo',

    CONSTRAINT FK_Carrito_Usuario
        FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario)
);
GO

CREATE TABLE CarritoDetalle (
    id_detalle INT IDENTITY(1,1) PRIMARY KEY,
    id_carrito INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad INT NOT NULL,
    subtotal DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_CarritoDetalle_Carrito
        FOREIGN KEY (id_carrito)
        REFERENCES Carrito(id_carrito),

    CONSTRAINT FK_CarritoDetalle_Producto
        FOREIGN KEY (id_producto)
        REFERENCES Producto(id_producto),

    CONSTRAINT UQ_CarritoDetalle_Carrito_Producto
        UNIQUE (id_carrito, id_producto)
);
GO

CREATE TABLE Pedido (
    id_pedido INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    id_direccion INT NULL,
    fecha_pedido DATETIME DEFAULT GETDATE(),
    estado VARCHAR(20) DEFAULT 'Activo',
    subtotal DECIMAL(10,2) NOT NULL DEFAULT 0,
    descuento DECIMAL(10,2) DEFAULT 0,
    total DECIMAL(10,2) NOT NULL,
    provincia_entrega VARCHAR(100) NULL,
    canton_entrega VARCHAR(100) NULL,
    distrito_entrega VARCHAR(100) NULL,
    detalles_entrega VARCHAR(255) NULL,

    CONSTRAINT FK_Pedido_Usuario
        FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario),

    CONSTRAINT FK_Pedido_Direccion
        FOREIGN KEY (id_direccion)
        REFERENCES Direccion(id_direccion)
);
GO

CREATE TABLE PedidoDetalle (
    id_detalle_pedido INT IDENTITY(1,1) PRIMARY KEY,
    id_pedido INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad INT NOT NULL,
    precio_unitario DECIMAL(10,2) NOT NULL,
    subtotal DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_PedidoDetalle_Pedido
        FOREIGN KEY (id_pedido)
        REFERENCES Pedido(id_pedido),

    CONSTRAINT FK_PedidoDetalle_Producto
        FOREIGN KEY (id_producto)
        REFERENCES Producto(id_producto)
);
GO

CREATE TABLE Promocion (
    id_promocion INT IDENTITY(1,1) PRIMARY KEY,
    nombre_promocion VARCHAR(100) NOT NULL,
    descripcion VARCHAR(MAX),
    porcentaje_descuento DECIMAL(5,2),
    fecha_inicio DATE,
    fecha_fin DATE,
    estado BIT DEFAULT 1
);
GO

CREATE TABLE ProductoPromocion (
    id_producto_promocion INT IDENTITY(1,1) PRIMARY KEY,
    id_producto INT NOT NULL,
    id_promocion INT NOT NULL,

    CONSTRAINT FK_ProductoPromocion_Producto
        FOREIGN KEY (id_producto)
        REFERENCES Producto(id_producto),

    CONSTRAINT FK_ProductoPromocion_Promocion
        FOREIGN KEY (id_promocion)
        REFERENCES Promocion(id_promocion),

    CONSTRAINT UQ_ProductoPromocion_Producto_Promocion
        UNIQUE (id_producto, id_promocion)
);
GO

CREATE TABLE HistorialPuntos (
    id_historial INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    id_pedido INT NULL,
    puntos_ganados INT DEFAULT 0,
    puntos_utilizados INT DEFAULT 0,
    fecha_movimiento DATETIME DEFAULT GETDATE(),
    descripcion VARCHAR(255),

    CONSTRAINT FK_HistorialPuntos_Usuario
        FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario),

    CONSTRAINT FK_HistorialPuntos_Pedido
        FOREIGN KEY (id_pedido)
        REFERENCES Pedido(id_pedido)
);
GO

/* ==========================================================
   BITÁCORA DE ERRORES
   Estructura equivalente a la utilizada en clase.
   ========================================================== */

CREATE TABLE ErrorSistema (
    id_error INT IDENTITY(1,1) PRIMARY KEY,
    mensaje VARCHAR(MAX) NOT NULL,
    fecha_hora DATETIME NOT NULL DEFAULT GETDATE(),
    lugar VARCHAR(100) NOT NULL,
    id_usuario INT NOT NULL DEFAULT 0
);
GO
CREATE PROCEDURE spRegistrarError
    @Mensaje VARCHAR(MAX),
    @FechaHora DATETIME,
    @Lugar VARCHAR(100),
    @IdUsuario INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ErrorSistema
    (
        mensaje,
        fecha_hora,
        lugar,
        id_usuario
    )
    VALUES
    (
        @Mensaje,
        @FechaHora,
        @Lugar,
        @IdUsuario
    );
END
GO

/* ==========================================================
   USUARIO INICIAL PARA PRUEBAS
  
   ========================================================== */

INSERT INTO Usuario
(
    nombre,
    apellido,
    correo,
    telefono,
    contrasena,
    tipo_usuario,
    estado
)
VALUES
(
    'Administrador',
    'Ágata',
    'admin@agata.com',
    '8888-8888',
    'Admin123*',
    'ADMIN',
    1
);
GO



GO

-- ================= CARGA DE DATOS =================

-- =====================================================================
-- CATALOGO BASE: Categorias, Marcas, Productos
-- =====================================================================

INSERT INTO Categoria (nombre_categoria, descripcion, estado) VALUES
('Rostro', 'Bases, correctores y polvos para el rostro', 1),
('Ojos', 'Sombras, delineadores y mascaras de pestanas', 1),
('Labios', 'Labiales, gloss y delineadores labiales', 1),
('Piel y Skincare', 'Cuidado facial: sueros, cremas y protector solar', 1),
('Cabello', 'Shampoo, acondicionador y tratamientos capilares', 1),
('Accesorios de Maquillaje', 'Brochas, esponjas y herramientas de aplicacion', 1);
GO

INSERT INTO Marca (nombre_marca, descripcion, estado) VALUES
('Maybelline', 'Marca internacional de maquillaje', 1),
('L''Oreal Paris', 'Marca internacional de belleza', 1),
('NYX Professional Makeup', 'Maquillaje profesional accesible', 1),
('Revlon', 'Marca clasica de cosmeticos', 1),
('e.l.f. Cosmetics', 'Cosmeticos de bajo costo y alta calidad', 1),
('Agata Beauty', 'Marca propia de Agata MakeUp', 1);
GO

INSERT INTO Producto (id_categoria, id_marca, nombre_producto, descripcion, precio, stock, imagen_url, estado) VALUES
(1, 1, 'Base de maquillaje Fit Me',            'Base liquida de cobertura media, acabado natural', 12000, 40, '/Content/img/product01.png', 1),
(1, 4, 'Corrector Instant Age Rewind',         'Corrector iluminador para ojeras',                  9500, 35, '/Content/img/product02.png', 1),
(1, 2, 'Polvo compacto Infallible',            'Polvo compacto de larga duracion',                 11000, 30, '/Content/img/product03.png', 1),
(2, 3, 'Paleta de sombras Ojos Nude',          'Paleta de 12 tonos neutros',                       15500, 25, '/Content/img/product04.png', 1),
(2, 1, 'Delineador de ojos Gel Liner',         'Delineador en gel resistente al agua',              6500, 50, '/Content/img/product05.png', 1),
(2, 2, 'Mascara de pestanas Lash Sensational', 'Mascara voluminizadora',                            8900, 45, '/Content/img/product06.png', 1),
(3, 3, 'Labial mate Matte Ink',                'Labial liquido mate de larga duracion',             7200, 60, '/Content/img/product07.png', 1),
(3, 1, 'Labial hidratante Super Stay',         'Labial de larga duracion con acabado hidratante',   8100, 55, '/Content/img/product08.png', 1),
(3, 5, 'Gloss labial Glazed Lip',              'Gloss con efecto cristalino',                       5400, 40, '/Content/img/product09.png', 1),
(4, 6, 'Serum facial Hydra Boost',             'Serum hidratante con acido hialuronico',           13500, 30, '/Content/img/hotdeal.png', 1),
(4, 2, 'Crema hidratante dia',                 'Crema hidratante de uso diario',                   14200, 28, '/Content/img/hotdeal.png', 1),
(4, 6, 'Protector solar SPF50',                'Proteccion solar facial no grasosa',                9800, 45, '/Content/img/hotdeal.png', 1),
(5, 6, 'Shampoo reparador',                    'Shampoo para cabello danado',                       8600, 32, '/Content/img/shop01.png', 1),
(5, 6, 'Acondicionador nutritivo',             'Acondicionador nutritivo para todo tipo de cabello',8600, 32, '/Content/img/shop02.png', 1),
(6, 5, 'Set de brochas profesionales',         'Set de 8 brochas para rostro y ojos',              18500, 20, '/Content/img/shop03.png', 1),
(6, 5, 'Esponja beauty blender',               'Esponja de maquillaje multiusos',                   4500, 60, '/Content/img/shop03.png', 1),
(1, 3, 'Rubor en polvo Baked Blush',           'Rubor horneado de larga duracion',                  6900, 38, '/Content/img/product03.png', 1),
(1, 5, 'Iluminador en barra Glow Stick',       'Iluminador en formato stick',                       7600, 33, '/Content/img/product01.png', 1);
GO

-- =====================================================================
-- USUARIOS (1 administrador + 5 clientes) Y DIRECCIONES
-- Contrasenas hasheadas con SHA2_256 (solo demostrativo; en produccion
-- usar el hashing propio de la capa de autenticacion, ej. ASP.NET Identity).
-- Password de ejemplo para todos: "Cliente123!" (admin: "Admin123!")
-- =====================================================================
INSERT INTO Usuario
(nombre, apellido, correo, telefono, contrasena, tipo_usuario, preferencias, puntos_acumulados, estado)
VALUES
('María Fernanda', 'Rojas Vargas',   'maria.rojas@example.com',   '8888-1111', 'Cliente123!', 'CLIENTE', 'Labios, Skincare, tonos nude',       30, 1),
('Carlos Andrés',  'Jiménez Solano', 'carlos.jimenez@example.com','8888-2222', 'Cliente123!', 'CLIENTE', 'Accesorios, rostro',                23, 1),
('Valeria',        'Castro Méndez',  'valeria.castro@example.com','8888-3333', 'Cliente123!', 'CLIENTE', 'Skincare, protección solar',         0, 1),
('Josué Alberto',  'Vargas Núñez',   'josue.vargas@example.com',  '8888-4444', 'Cliente123!', 'CLIENTE', 'Ojos, paletas de sombras',           0, 1),
('Camila Sofía',   'Herrera López',  'camila.herrera@example.com','8888-5555', 'Cliente123!', 'CLIENTE', 'Ojos, labios',                       0, 1);
GO

-- La tabla Direccion permite solo una dirección por usuario porque id_usuario es UNIQUE.
INSERT INTO Direccion (id_usuario, provincia, canton, distrito, detalles) VALUES
(2, 'San José', 'Escazú',     'San Rafael', 'Residencial Los Laureles, casa 12'),
(3, 'Heredia',  'Heredia',    'Mercedes',   'Del parque 300 m al norte'),
(4, 'Alajuela', 'Alajuela',   'San Rafael', 'Barrio San Rafael, casa azul'),
(5, 'Cartago',  'Cartago',    'Oriental',   'Frente a la iglesia'),
(6, 'San José', 'Curridabat', 'Tirrases',   'Condominio Vista Verde, apartamento 3B');
GO

-- =====================================================================
-- FAVORITOS
-- =====================================================================

INSERT INTO Favorito (id_usuario, id_producto, fecha_agregado) VALUES
(2, 9,  '2026-06-01'),
(2, 7,  '2026-06-05'),
(3, 1,  '2026-04-01'),
(3, 15, '2026-04-01'),
(4, 10, '2026-05-20'),
(4, 12, '2026-05-20'),
(5, 4,  '2026-03-01'),
(6, 6,  '2026-07-02'),
(6, 8,  '2026-07-02');
GO

-- =====================================================================
-- CARRITOS ACTIVOS (compras en curso, aun no confirmadas)
-- =====================================================================

INSERT INTO Carrito (id_usuario, fecha_creacion, estado_carrito) VALUES
(2, '2026-07-14', 'Activo'),
(3, '2026-07-15', 'Activo');
GO

INSERT INTO CarritoDetalle (id_carrito, id_producto, cantidad, subtotal) VALUES
(1, 9,  1,  5400.00),
(1, 16, 2,  9000.00),
(2, 5,  1,  6500.00);
GO

-- =====================================================================
-- PROMOCIONES
-- =====================================================================

INSERT INTO Promocion (nombre_promocion, descripcion, porcentaje_descuento, fecha_inicio, fecha_fin, estado) VALUES
('Verano Radiante',       'Descuento en productos de skincare seleccionados', 15.00, '2026-06-01', '2026-08-31', 1),
('Black Friday Belleza',  'Descuento especial de fin de ano en labiales',     25.00, '2025-11-24', '2025-11-30', 0);
GO

INSERT INTO ProductoPromocion (id_producto, id_promocion) VALUES
(1, 1),
(10, 1),
(11, 1),
(12, 1),
(7, 2),
(8, 2);
GO

-- =====================================================================
-- PEDIDOS HISTORICOS (para historial, seguimiento, reportes y puntos)
-- id_pedido esperado: 1..6 (respetar este orden de insercion)
-- =====================================================================

INSERT INTO Pedido
(id_usuario, fecha_pedido, estado, subtotal, descuento, total)
VALUES
(2, '2026-05-10', 'Entregado',  20900.00,    0.00, 20900.00),
(2, '2026-06-20', 'Entregado',  10800.00,    0.00, 10800.00),
(3, '2026-04-15', 'Entregado',  23000.00,    0.00, 23000.00),
(4, '2026-07-01', 'Enviado',    23300.00, 3495.00, 19805.00),
(5, '2026-03-05', 'Cancelado',  15500.00,    0.00, 15500.00),
(6, '2026-07-10', 'Registrado', 17000.00,    0.00, 17000.00);
GO

INSERT INTO PedidoDetalle
(id_pedido, id_producto, cantidad, precio_unitario, subtotal)
VALUES
(1, 1,  1, 12000.00, 12000.00),
(1, 6,  1,  8900.00,  8900.00),
(2, 9,  2,  5400.00, 10800.00),
(3, 15, 1, 18500.00, 18500.00),
(3, 16, 1,  4500.00,  4500.00),
(4, 10, 1, 13500.00, 13500.00),
(4, 12, 1,  9800.00,  9800.00),
(5, 4,  1, 15500.00, 15500.00),
(6, 6,  1,  8900.00,  8900.00),
(6, 8,  1,  8100.00,  8100.00);
GO

-- =====================================================================
-- HISTORIAL DE PUNTOS (solo pedidos en estado 'Entregado')
-- Regla usada: 1 punto por cada 1000 colones del total
-- =====================================================================

INSERT INTO HistorialPuntos (id_usuario, id_pedido, puntos_ganados, puntos_utilizados, fecha_movimiento, descripcion) VALUES
(2, 1, 20, 0, '2026-05-12', 'Puntos por compra - Pedido #1'),
(2, 2, 10, 0, '2026-06-22', 'Puntos por compra - Pedido #2'),
(3, 3, 23, 0, '2026-04-17', 'Puntos por compra - Pedido #3');
GO

-- =====================================================================
-- MOVIMIENTOS DE INVENTARIO Y NOTIFICACIONES
-- =====================================================================
-- No se incluyen en esta versión porque las tablas MovimientoInventario
-- y Notificacion no forman parte del modelo actual de Entity Framework.
-- Si se implementan posteriormente, deben agregarse mediante otra migración.
GO

INSERT INTO Usuario
(
    nombre,
    apellido,
    correo,
    telefono,
    contrasena,
    tipo_usuario,
    preferencias,
    puntos_acumulados,
    fecha_registro,
    fecha_actualizacion,
    tiene_contrasena_temporal,
    vigencia_contrasena_temporal,
    estado
)
VALUES
(
    'María',
    'Rodríguez',
    'maria.rodriguez@correo.com',
    '8888-7777',
    'Cliente123*',
    'CLIENTE',
    'Maquillaje y cuidado facial',
    0,
    GETDATE(),
    NULL,
    0,
    NULL,
    1
);
GO