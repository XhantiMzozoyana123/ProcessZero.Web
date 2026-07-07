#!/bin/bash
# ProcessZero - Setup Environment Variables for Production
# Run this on your Linux VPS to set up environment variables for Twilio and other credentials

set -e

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║   ProcessZero - Environment Variables Setup (Linux VPS)       ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then
   echo "⚠️  Run with sudo: sudo ./setup-env-vars.sh"
   exit 1
fi

echo "🔐 Setting up environment variables for ProcessZero..."
echo ""

# Prompt for credentials
read -p "Enter Twilio Account SID (ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx): " TWILIO_SID
read -p "Enter Twilio Auth Token: " TWILIO_TOKEN
read -p "Enter Twilio Phone Number (+1...): " TWILIO_PHONE

read -p "Enter Cal.com API Key (optional, press Enter to skip): " CAL_API_KEY
read -p "Enter Paystack Secret Key (optional, press Enter to skip): " PAYSTACK_KEY
read -p "Enter PayFast Merchant ID (optional, press Enter to skip): " PAYFAST_ID
read -p "Enter PayFast Merchant Key (optional, press Enter to skip): " PAYFAST_KEY
read -p "Enter PayFast Passphrase (optional, press Enter to skip): " PAYFAST_PASS
read -p "Enter Google OAuth Client ID (optional, press Enter to skip): " GOOGLE_ID
read -p "Enter Google OAuth Client Secret (optional, press Enter to skip): " GOOGLE_SECRET
read -p "Enter JWT Key (optional, press Enter to skip): " JWT_KEY

# Create systemd environment file
echo ""
echo "📝 Creating systemd environment file..."

cat > /etc/environment.d/processzero.conf << EOF
# ProcessZero Environment Variables
# Set on: $(date)

TWILIO_ACCOUNT_SID="$TWILIO_SID"
TWILIO_AUTH_TOKEN="$TWILIO_TOKEN"
TWILIO_PHONE_NUMBER="$TWILIO_PHONE"

CAL_API_KEY="$CAL_API_KEY"
PAYSTACK_SECRET_KEY="$PAYSTACK_KEY"
PAYFAST_MERCHANT_ID="$PAYFAST_ID"
PAYFAST_MERCHANT_KEY="$PAYFAST_KEY"
PAYFAST_PASSPHRASE="$PAYFAST_PASS"
GOOGLE_CLIENT_ID="$GOOGLE_ID"
GOOGLE_CLIENT_SECRET="$GOOGLE_SECRET"
JWT_KEY="$JWT_KEY"
EOF

chmod 600 /etc/environment.d/processzero.conf

echo "✅ Environment variables saved to /etc/environment.d/processzero.conf"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📋 Next steps:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "1. Verify environment variables were set:"
echo "   $ source /etc/environment.d/processzero.conf"
echo "   $ echo \$TWILIO_ACCOUNT_SID"
echo ""
echo "2. Update your deployment to use Docker Compose:"
echo "   $ cd /path/to/ProcessZero.Web"
echo "   $ git pull origin master"
echo "   $ docker compose down && docker compose up -d --build"
echo ""
echo "3. Verify the app is running:"
echo "   $ docker compose logs -f"
echo ""
echo "4. Or if using Kubernetes (simple method):"
echo "   $ docker build -t ghcr.io/xhantiMzozoyana123/processzero-web:1.0.0 ."
echo "   $ docker push ghcr.io/xhantiMzozoyana123/processzero-web:1.0.0"
echo "   $ kubectl set env deployment/processzero-web --from=configmap=processzero-env -n processzero"
echo ""
echo "✨ Done! Your environment variables are now set."
echo ""
