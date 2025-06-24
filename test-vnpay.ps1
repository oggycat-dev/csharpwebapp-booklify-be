# Test VNPay Payment Creation
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

$body = @{
    orderId = "TEST001"
    orderDescription = "Test thanh toan"
    amount = 50000
    language = "vn"
} | ConvertTo-Json

Write-Host "Testing VNPay Payment Creation..." -ForegroundColor Green
Write-Host "Request Body: $body" -ForegroundColor Yellow

try {
    # Skip certificate validation for local HTTPS
    add-type @"
        using System.Net;
        using System.Security.Cryptography.X509Certificates;
        public class TrustAllCertsPolicy : ICertificatePolicy {
            public bool CheckValidationResult(
                ServicePoint srvPoint, X509Certificate certificate,
                WebRequest request, int certificateProblem) {
                return true;
            }
        }
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    
    $response = Invoke-RestMethod -Uri "http://localhost:5124/api/payment/vnpay/create-payment" -Method POST -Headers $headers -Body $body
    
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Payment URL: $($response.paymentUrl)" -ForegroundColor Cyan
    Write-Host "Order ID: $($response.orderId)" -ForegroundColor Cyan
    Write-Host "Transaction Ref: $($response.transactionRef)" -ForegroundColor Cyan
    
    # Copy payment URL to clipboard if possible
    if (Get-Command Set-Clipboard -ErrorAction SilentlyContinue) {
        $response.paymentUrl | Set-Clipboard
        Write-Host "Payment URL copied to clipboard!" -ForegroundColor Magenta
    }
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "`nTesting Debug Endpoint..." -ForegroundColor Green

$debugBody = @{
    orderDescription = "Debug test"
    amount = 50000
    language = "vn"
} | ConvertTo-Json

try {
    $debugResponse = Invoke-RestMethod -Uri "http://localhost:5124/api/payment/vnpay/debug-signature" -Method POST -Headers $headers -Body $debugBody
    
    Write-Host "DEBUG SUCCESS!" -ForegroundColor Green
    Write-Host "Hash Data: $($debugResponse.debug.hashData)" -ForegroundColor Yellow
    Write-Host "Hash Secret: $($debugResponse.debug.hashSecret)" -ForegroundColor Yellow
    Write-Host "Secure Hash: $($debugResponse.debug.secureHash)" -ForegroundColor Yellow
}
catch {
    Write-Host "DEBUG ERROR: $($_.Exception.Message)" -ForegroundColor Red
} 