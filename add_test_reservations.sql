-- Add 5 Test Reservations with different statuses
-- First, let's get a user and product to use

DECLARE @UserId NVARCHAR(450);
DECLARE @ProductId INT;
DECLARE @ProductPrice DECIMAL(18,2);
DECLARE @ProductCurrency NVARCHAR(10);
DECLARE @ProductSlug NVARCHAR(200);

-- Get first available user
SELECT TOP 1 @UserId = Id FROM AspNetUsers;

-- Get first available product
SELECT TOP 1
    @ProductId = Id,
    @ProductPrice = Price,
    @ProductCurrency = Currency,
    @ProductSlug = Slug
FROM StoreProducts
WHERE IsDeleted = 0;

-- Print info
PRINT 'Using User ID: ' + ISNULL(@UserId, 'NULL');
PRINT 'Using Product ID: ' + CAST(ISNULL(@ProductId, 0) AS NVARCHAR);
PRINT 'Product Price: ' + CAST(ISNULL(@ProductPrice, 0) AS NVARCHAR);

-- Only proceed if we have both user and product
IF @UserId IS NOT NULL AND @ProductId IS NOT NULL
BEGIN
    -- Delete existing test reservations for this user/product combination (clean slate)
    DELETE FROM StoreReservations WHERE UserId = @UserId AND ProductId = @ProductId;

    -- 1. PENDING - New reservation (waiting for contact)
    INSERT INTO StoreReservations (
        UserId, ProductId, Quantity, UnitPrice, TotalPrice, Currency, Slug,
        Status, PaymentStatus, DeliveryStatus,
        CreatedAt, UpdatedAt
    ) VALUES (
        @UserId, @ProductId, 1, @ProductPrice, @ProductPrice, @ProductCurrency,
        CONCAT('res-pending-', NEWID()),
        'pending', 'unpaid', 'pending',
        DATEADD(HOUR, -5, GETUTCDATE()), DATEADD(HOUR, -5, GETUTCDATE())
    );
    PRINT 'Inserted reservation 1: PENDING';

    -- 2. CONTACTED - Admin contacted the customer
    INSERT INTO StoreReservations (
        UserId, ProductId, Quantity, UnitPrice, TotalPrice, Currency, Slug,
        Status, PaymentStatus, DeliveryStatus,
        ContactedAt, AdminNotes,
        CreatedAt, UpdatedAt
    ) VALUES (
        @UserId, @ProductId, 2, @ProductPrice, @ProductPrice * 2, @ProductCurrency,
        CONCAT('res-contacted-', NEWID()),
        'contacted', 'unpaid', 'pending',
        DATEADD(HOUR, -3, GETUTCDATE()), N'تم التواصل مع العميل - سيدفع فودافون كاش',
        DATEADD(HOUR, -4, GETUTCDATE()), DATEADD(HOUR, -3, GETUTCDATE())
    );
    PRINT 'Inserted reservation 2: CONTACTED';

    -- 3. CONTACTED + PAID - Payment received, waiting for delivery
    INSERT INTO StoreReservations (
        UserId, ProductId, Quantity, UnitPrice, TotalPrice, Currency, Slug,
        Status, PaymentStatus, DeliveryStatus,
        ContactedAt, PaymentMethod, PaymentReference, PaidAmount, PaidAt, AdminNotes,
        CreatedAt, UpdatedAt
    ) VALUES (
        @UserId, @ProductId, 1, @ProductPrice, @ProductPrice, @ProductCurrency,
        CONCAT('res-paid-', NEWID()),
        'contacted', 'paid', 'pending',
        DATEADD(HOUR, -6, GETUTCDATE()), 'vodafone_cash', 'TXN123456789', @ProductPrice, DATEADD(HOUR, -2, GETUTCDATE()),
        N'تم استلام المبلغ كاملاً - جاري تجهيز الطلب',
        DATEADD(HOUR, -8, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE())
    );
    PRINT 'Inserted reservation 3: PAID';

    -- 4. CONTACTED + PAID + DELIVERED - Delivered, waiting for completion
    INSERT INTO StoreReservations (
        UserId, ProductId, Quantity, UnitPrice, TotalPrice, Currency, Slug,
        Status, PaymentStatus, DeliveryStatus,
        ContactedAt, PaymentMethod, PaymentReference, PaidAmount, PaidAt,
        DeliveredAt, DeliveryNotes, AdminNotes,
        CreatedAt, UpdatedAt
    ) VALUES (
        @UserId, @ProductId, 1, @ProductPrice, @ProductPrice, @ProductCurrency,
        CONCAT('res-delivered-', NEWID()),
        'contacted', 'paid', 'delivered',
        DATEADD(DAY, -2, GETUTCDATE()), 'bank_transfer', 'BANK-REF-456', @ProductPrice, DATEADD(DAY, -2, GETUTCDATE()),
        DATEADD(HOUR, -1, GETUTCDATE()), N'تم التوصيل للعنوان: المعادي - شارع 9 - عمارة 15',
        N'العميل استلم بنفسه',
        DATEADD(DAY, -3, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE())
    );
    PRINT 'Inserted reservation 4: DELIVERED';

    -- 5. COMPLETED - Fully completed reservation
    INSERT INTO StoreReservations (
        UserId, ProductId, Quantity, UnitPrice, TotalPrice, Currency, Slug,
        Status, PaymentStatus, DeliveryStatus,
        ContactedAt, PaymentMethod, PaymentReference, PaidAmount, PaidAt,
        DeliveredAt, DeliveryNotes, AdminNotes,
        CreatedAt, UpdatedAt
    ) VALUES (
        @UserId, @ProductId, 3, @ProductPrice, @ProductPrice * 3, @ProductCurrency,
        CONCAT('res-completed-', NEWID()),
        'completed', 'paid', 'delivered',
        DATEADD(DAY, -7, GETUTCDATE()), 'instapay', 'INSTA-789', @ProductPrice * 3, DATEADD(DAY, -6, GETUTCDATE()),
        DATEADD(DAY, -5, GETUTCDATE()), N'تم التوصيل بنجاح',
        N'طلب مكتمل - العميل راضي',
        DATEADD(DAY, -10, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE())
    );
    PRINT 'Inserted reservation 5: COMPLETED';

    -- Show results
    SELECT
        r.Id,
        r.Quantity,
        r.TotalPrice,
        r.Status,
        r.PaymentStatus,
        r.DeliveryStatus,
        r.PaymentMethod,
        r.CreatedAt
    FROM StoreReservations r
    WHERE r.UserId = @UserId
    ORDER BY r.CreatedAt DESC;

    PRINT '';
    PRINT '✅ Successfully added 5 test reservations!';
END
ELSE
BEGIN
    PRINT '❌ Error: No user or product found in database';
    PRINT 'Please create at least one user and one product first.';
END
