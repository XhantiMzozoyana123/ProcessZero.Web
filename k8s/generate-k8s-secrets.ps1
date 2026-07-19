#!/usr/bin/env pwsh
<#
.SYNOPSIS
Helper script to generate base64-encoded secrets for Kubernetes

.DESCRIPTION
This script prompts for your actual secrets and generates base64-encoded values
that you can paste into k8s/secrets.yaml

.EXAMPLE
./generate-k8s-secrets.ps1
#>

Write-Host "=== ProcessZero Kubernetes Secrets Generator ===" -ForegroundColor Cyan
Write-Host "This will help you generate base64-encoded secrets for Kubernetes" -ForegroundColor Yellow
Write-Host ""

function Encode-Secret {
	param(
		[string]$PromptText,
		[string]$SecretName
	)

	$value = Read-Host -Prompt $PromptText -AsSecureString
	$plainValue = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToCoTaskMemUnicode($value))

	if ([string]::IsNullOrWhiteSpace($plainValue)) {
		Write-Host "⏭️  Skipping $SecretName (empty)" -ForegroundColor Gray
		return
	}

	$base64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($plainValue))

	Write-Host ""
	Write-Host "✅ $SecretName" -ForegroundColor Green
	Write-Host "  $base64" -ForegroundColor Cyan
	Write-Host "  (Copied to clipboard)" -ForegroundColor Green

	$base64 | Set-Clipboard
	Write-Host ""
}

Write-Host "📝 Enter your secrets (or press Enter to skip)" -ForegroundColor Yellow
Write-Host ""

Encode-Secret "Enter Twilio Account SID (ACxxxxxx...)" "Twilio__AccountSid"
Encode-Secret "Enter Twilio Auth Token" "Twilio__AuthToken"
Encode-Secret "Enter Twilio Phone Number (+1...)" "Twilio__PhoneNumber"
Encode-Secret "Enter Cal.com API Key" "CalOptions__ApiKey"
Encode-Secret "Enter Paystack Secret Key" "Paystack__SecretKey"
Encode-Secret "Enter PayFast Merchant ID" "PayFast__MerchantId"
Encode-Secret "Enter PayFast Merchant Key" "PayFast__MerchantKey"
Encode-Secret "Enter PayFast Passphrase" "PayFast__Passphrase"
Encode-Secret "Enter Google OAuth Client ID" "GoogleOAuth__ClientId"
Encode-Secret "Enter Google OAuth Client Secret" "GoogleOAuth__ClientSecret"
Encode-Secret "Enter JWT Key" "Jwt__Key"

Write-Host ""
Write-Host "✨ Done! Next steps:" -ForegroundColor Cyan
Write-Host "1. Edit k8s/secrets.yaml and replace the base64 values"
Write-Host "2. Deploy to Kubernetes: kubectl apply -f k8s/secrets.yaml"
Write-Host ""
