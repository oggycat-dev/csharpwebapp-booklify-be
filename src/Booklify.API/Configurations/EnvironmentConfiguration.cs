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
        builder.Configuration["Storage:ProviderType"] = 
            Environment.GetEnvironmentVariable("STORAGE_PROVIDER_TYPE") ?? "LocalStorage";
        builder.Configuration["Storage:BaseUrl"] = 
            Environment.GetEnvironmentVariable("STORAGE_BASE_URL") ?? string.Empty;
            
        // Local Storage Settings
        builder.Configuration["Storage:LocalStorage:RootPath"] = 
            Environment.GetEnvironmentVariable("LOCAL_STORAGE_ROOT_PATH") ?? "wwwroot/uploads";
        builder.Configuration["Storage:LocalStorage:MaxFileSize"] = 
            Environment.GetEnvironmentVariable("LOCAL_STORAGE_MAX_FILE_SIZE") ?? "10485760"; // 10MB
            
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
            Environment.GetEnvironmentVariable("AWS_S3_MAX_FILE_SIZE") ?? "52428800"; // 50MB

        // VNPay Settings
        builder.Configuration["VNPay:TmnCode"] = 
            Environment.GetEnvironmentVariable("VNPAY_TMN_CODE") ?? "2QXUI4J4";
        builder.Configuration["VNPay:HashSecret"] = 
            Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET") ?? "SECRETKEY123456789";
        builder.Configuration["VNPay:PaymentUrl"] = 
            Environment.GetEnvironmentVariable("VNPAY_PAYMENT_URL") ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        builder.Configuration["VNPay:ReturnUrl"] = 
            Environment.GetEnvironmentVariable("VNPAY_RETURN_URL") ?? "https://localhost:5123/api/payment/vnpay/return";
        builder.Configuration["VNPay:TimeoutInMinutes"] = 
            Environment.GetEnvironmentVariable("VNPAY_TIMEOUT_MINUTES") ?? "15";

        return builder;
    }
} 