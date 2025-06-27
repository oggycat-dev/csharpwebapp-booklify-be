namespace Booklify.API.Configurations;

public static class EnvironmentConfiguration
{
    public static WebApplicationBuilder AddEnvironmentConfiguration(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;
        
        // Load appropriate .env file based on environment
        if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            //DotNetEnv.Env.Load(".env.production");
            builder.Configuration["App:Environment"] = "Production";
        }
        else
        {
            DotNetEnv.Env.Load();
            builder.Configuration["App:Environment"] = environment;
        }
        
        // Load environment variables into Configuration
        
        // Add new environment variables
        builder.Configuration["Security:RequireHttps"] = 
            Environment.GetEnvironmentVariable("REQUIRE_HTTPS") ?? "false";
        builder.Configuration["Cors:AllowAnyOrigin"] = 
            Environment.GetEnvironmentVariable("CORS_ALLOW_ANY_ORIGIN") ?? "true";
        
        // Database Connection
        builder.Configuration["ConnectionStrings:DefaultConnection"] = 
            Environment.GetEnvironmentVariable("CONNECTION_STRING");
            
        // JWT Settings    
        builder.Configuration["Jwt:Secret"] = 
            Environment.GetEnvironmentVariable("JWT_SECRET");
        builder.Configuration["Jwt:Issuer"] = 
            Environment.GetEnvironmentVariable("JWT_ISSUER");
        builder.Configuration["Jwt:Audience"] = 
            Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        builder.Configuration["Jwt:ExpiresInMinutes"] = 
            Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_MINUTES");
        builder.Configuration["Jwt:RefreshTokenExpiresInDays"] = 
            Environment.GetEnvironmentVariable("REFRESH_TOKEN_EXPIRES_IN_DAYS");

        // Frontend URL
        builder.Configuration["FrontendUrl"] = 
            Environment.GetEnvironmentVariable("FRONTEND_URL");
            
        // Load allowed origins for CORS from FRONTEND_URL
        string corsOriginsString = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? string.Empty;
        if (!string.IsNullOrEmpty(corsOriginsString))
        {
            var origins = corsOriginsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            builder.Configuration["Cors:AllowedOrigins"] = string.Join(',', origins);
        }

        // System Settings
        builder.Configuration["SystemName"] = 
            Environment.GetEnvironmentVariable("SYSTEM_NAME") ?? "Booklify";

        // Storage Settings
        // General storage settings
        builder.Configuration["Storage:ProviderType"] = 
            Environment.GetEnvironmentVariable("STORAGE_PROVIDER_TYPE") ?? "LocalStorage";
        builder.Configuration["Storage:BaseUrl"] = 
            Environment.GetEnvironmentVariable("STORAGE_BASE_URL") ?? string.Empty;
            
        // Hangfire Settings
        builder.Configuration["Hangfire:ConnectionString"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_CONNECTION_STRING") ?? string.Empty;
        builder.Configuration["Hangfire:WorkerCount"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_WORKER_COUNT") ?? "0"; // 0 means Environment.ProcessorCount * 2
        builder.Configuration["Hangfire:HeartbeatInterval"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_HEARTBEAT_INTERVAL") ?? "30"; // seconds
        builder.Configuration["Hangfire:QueuePollInterval"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_QUEUE_POLL_INTERVAL") ?? "0"; // seconds
        builder.Configuration["Hangfire:CommandBatchMaxTimeout"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_COMMAND_BATCH_MAX_TIMEOUT") ?? "300"; // seconds (5 minutes)
        builder.Configuration["Hangfire:SlidingInvisibilityTimeout"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_SLIDING_INVISIBILITY_TIMEOUT") ?? "300"; // seconds (5 minutes)
        builder.Configuration["Hangfire:UseRecommendedIsolationLevel"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_USE_RECOMMENDED_ISOLATION_LEVEL") ?? "true";
        builder.Configuration["Hangfire:DisableGlobalLocks"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_DISABLE_GLOBAL_LOCKS") ?? "true";
        builder.Configuration["Hangfire:Queues"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_QUEUES") ?? "default,file-operations,epub-processing";
            
        // Hangfire Job Schedules
        builder.Configuration["Hangfire:Jobs:Cleanup:Cron"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_CLEANUP_CRON") ?? "0 3 * * 0"; // Every Sunday at 3 AM
        builder.Configuration["Hangfire:Jobs:Cleanup:RetentionDays"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_CLEANUP_RETENTION_DAYS") ?? "30"; // 30 days retention

        // Retry Policy Settings
        builder.Configuration["Hangfire:Retry:Attempts"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_RETRY_ATTEMPTS") ?? "3";
        builder.Configuration["Hangfire:Retry:DelayInSeconds:First"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_RETRY_DELAY_FIRST") ?? "60";  // 1 phút
        builder.Configuration["Hangfire:Retry:DelayInSeconds:Second"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_RETRY_DELAY_SECOND") ?? "300"; // 5 phút
        builder.Configuration["Hangfire:Retry:DelayInSeconds:Third"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_RETRY_DELAY_THIRD") ?? "600"; // 10 phút

        // Job Execution Limits
        builder.Configuration["Hangfire:Limits:MaxConcurrentJobsPerQueue"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_MAX_CONCURRENT_JOBS_PER_QUEUE") ?? "5";
        builder.Configuration["Hangfire:Limits:QueueTimeout"] = 
            Environment.GetEnvironmentVariable("HANGFIRE_QUEUE_TIMEOUT") ?? "600"; // 10 phút
        // Local Storage Settings
        builder.Configuration["Storage:LocalStorage:RootPath"] = 
            Environment.GetEnvironmentVariable("LOCAL_STORAGE_ROOT_PATH") ?? "wwwroot/uploads";
        builder.Configuration["Storage:LocalStorage:MaxFileSize"] = 
            Environment.GetEnvironmentVariable("LOCAL_STORAGE_MAX_FILE_SIZE") ?? "524288000"; // 500MB
            
        // Amazon S3 Settings
        builder.Configuration["Storage:AmazonS3:AccessKey"] = 
            Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? string.Empty;
        builder.Configuration["Storage:AmazonS3:SecretKey"] = 
            Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? string.Empty;
        builder.Configuration["Storage:AmazonS3:BucketName"] = 
            Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME") ?? string.Empty;
        builder.Configuration["Storage:AmazonS3:Region"] = 
            Environment.GetEnvironmentVariable("AWS_S3_REGION") ?? "us-east-1";
        builder.Configuration["Storage:AmazonS3:UseHttps"] = 
            Environment.GetEnvironmentVariable("AWS_S3_USE_HTTPS") ?? "true";
        builder.Configuration["Storage:AmazonS3:MaxFileSize"] = 
            Environment.GetEnvironmentVariable("AWS_S3_MAX_FILE_SIZE") ?? "524288000"; // 500MB
            
        // S3 Multipart Upload Settings
        builder.Configuration["Storage:AmazonS3:MultipartThreshold"] = 
            Environment.GetEnvironmentVariable("AWS_S3_MULTIPART_THRESHOLD") ?? "104857600"; // 100MB
        builder.Configuration["Storage:AmazonS3:PartSize"] = 
            Environment.GetEnvironmentVariable("AWS_S3_PART_SIZE") ?? "10485760"; // 10MB
        builder.Configuration["Storage:AmazonS3:MaxConcurrentParts"] = 
            Environment.GetEnvironmentVariable("AWS_S3_MAX_CONCURRENT_PARTS") ?? "5";

        // VNPay Settings
        builder.Configuration["VNPay:TmnCode"] = 
            Environment.GetEnvironmentVariable("VNPAY_TMN_CODE") ?? "2QXUI4J4";
        builder.Configuration["VNPay:HashSecret"] = 
            Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET") ?? "SECRETKEY123456789";
        builder.Configuration["VNPay:PaymentUrl"] = 
            Environment.GetEnvironmentVariable("VNPAY_PAYMENT_URL") ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        builder.Configuration["VNPay:ReturnUrl"] = 
            Environment.GetEnvironmentVariable("VNPAY_RETURN_URL") ?? "https://localhost:7167/api/payment/vnpay/return";
        builder.Configuration["VNPay:Version"] = 
            Environment.GetEnvironmentVariable("VNPAY_VERSION") ?? "2.1.0";
        builder.Configuration["VNPay:Command"] = 
            Environment.GetEnvironmentVariable("VNPAY_COMMAND") ?? "pay";
        builder.Configuration["VNPay:CurrencyCode"] = 
            Environment.GetEnvironmentVariable("VNPAY_CURRENCY_CODE") ?? "VND";
        builder.Configuration["VNPay:OrderType"] = 
            Environment.GetEnvironmentVariable("VNPAY_ORDER_TYPE") ?? "other";
        builder.Configuration["VNPay:TimeZone"] = 
            Environment.GetEnvironmentVariable("VNPAY_TIME_ZONE") ?? "Asia/Ho_Chi_Minh";
        builder.Configuration["VNPay:TimeoutInMinutes"] = 
            Environment.GetEnvironmentVariable("VNPAY_TIMEOUT_MINUTES") ?? "15";

        return builder;
    }
} 