#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# ProcessZero VPS environment setup
# This writes one Docker env file used by docker-compose.yml
# =============================================================================

ENV_FILE=".processzero.env"

echo "============================================================"
echo "ProcessZero VPS environment setup"
echo "This writes one Docker env file used by docker-compose.yml"
echo "============================================================"
echo ""
echo "Enter the values once. Press Enter to keep the current/default value."
echo ""

# --- Helper: prompt with default ---
prompt() {
  local label="$1"
  local default="$2"
  local val
  read -r -p "$label [$default]: " val
  if [ -z "$val" ]; then
    val="$default"
  fi
  echo "$val"
}

# --- Database ---
DB_CONN=$(prompt "Database connection string" "Server=46.202.170.203;Port=3306;Database=processzero;User=xhanti;Password=Xhanti123!;")

# --- JWT ---
JWT_KEY=$(prompt "JWT key" "w95Tjv7Yq8L2dFpR4xN6mKbQ1sZcV3GhUa9XeJ5rPn8WyMt2HvLc0DfSk7BgRiYq")
JWT_ISSUER=$(prompt "JWT issuer" "ProcessZero.Api")
JWT_AUDIENCE=$(prompt "JWT audience" "ProcessZero.Client")

# --- Cal.com ---
CAL_BASE_URL=$(prompt "Cal.com base URL" "https://api.cal.com/v2")
CAL_API_KEY=$(prompt "Cal.com API key" "cal_live_3f66e867f9bfc782c41f41895479ecf3")

# --- Paystack ---
PAYSTACK_SECRET=$(prompt "Paystack secret key" "")

# --- PayFast ---
PAYFAST_MERCHANT_ID=$(prompt "PayFast merchant ID" "")
PAYFAST_MERCHANT_KEY=$(prompt "PayFast merchant key" "")
PAYFAST_PASSPHRASE=$(prompt "PayFast passphrase" "")
PAYFAST_SANDBOX=$(prompt "PayFast use sandbox (true/false)" "true")

# --- Assessment ---
ASSESSMENT_PASS_MARK=$(prompt "Assessment pass mark" "70.0")

# --- Payroll ---
PAYROLL_COMMISSION=$(prompt "Payroll commission rate" "20")

# --- Data Protection ---
DATA_PROTECTION_BANK=$(prompt "Data protection purpose for bank account" "ProcessZero.BankAccountService.V1")

# --- LLM ---
LLM_MODEL=$(prompt "LLM model" "llama3:latest")

# --- CORS ---
CORS_1=$(prompt "CORS origin 1" "http://localhost:3000")
CORS_2=$(prompt "CORS origin 2" "http://localhost:5173")
CORS_3=$(prompt "CORS origin 3" "http://77.93.155.211")
CORS_4=$(prompt "CORS origin 4" "https://77.93.155.211")
CORS_5=$(prompt "CORS origin 5" "https://processzero.xyz")
CORS_6=$(prompt "CORS origin 6" "https://www.processzero.xyz")

# --- Google OAuth ---
GOOGLE_CLIENT_ID=$(prompt "Google OAuth client ID" "")
GOOGLE_CLIENT_SECRET=$(prompt "Google OAuth client secret" "")
GOOGLE_REDIRECT_URI=$(prompt "Google OAuth redirect URI" "https://api.processzero.xyz/api/googleauth/callback")

# --- Twilio ---
TWILIO_SID=$(prompt "Twilio account SID" "")
TWILIO_TOKEN=$(prompt "Twilio auth token" "")
TWILIO_PHONE=$(prompt "Twilio phone number" "")

# --- Relay ---
RELAY_BASE_URL=$(prompt "Relay public base URL" "https://api.processzero.xyz")
RELAY_START=$(prompt "Relay send window start hour" "0")
RELAY_END=$(prompt "Relay send window end hour" "24")
RELAY_WEEKENDS=$(prompt "Relay send on weekends (true/false)" "true")
RELAY_JITTER=$(prompt "Relay jitter skip percent" "0")

# --- Timer Service (CRITICAL — must match in both services) ---
TIMER_API_KEY=$(prompt "Timer service API key (shared with main API)" "NjZmMGNhZjMtNTMyNS00YmUwLTgxYmEtNGY4YzM2YWRjNzc5M2QyYjZiYjQtNWY2")

# --- Write env file ---
cat > "$ENV_FILE" <<EOF
# ── Database ──
CONNECTION_STRING=${DB_CONN}

# ── JWT ──
JWT_KEY=${JWT_KEY}
JWT_ISSUER=${JWT_ISSUER}
JWT_AUDIENCE=${JWT_AUDIENCE}

# ── Cal.com ──
CAL_BASE_URL=${CAL_BASE_URL}
CAL_API_KEY=${CAL_API_KEY}

# ── Paystack ──
PAYSTACK_SECRET=${PAYSTACK_SECRET}

# ── PayFast ──
PAYFAST_MERCHANT_ID=${PAYFAST_MERCHANT_ID}
PAYFAST_MERCHANT_KEY=${PAYFAST_MERCHANT_KEY}
PAYFAST_PASSPHRASE=${PAYFAST_PASSPHRASE}
PAYFAST_SANDBOX=${PAYFAST_SANDBOX}

# ── Assessment ──
ASSESSMENT_PASS_MARK=${ASSESSMENT_PASS_MARK}

# ── Payroll ──
PAYROLL_COMMISSION=${PAYROLL_COMMISSION}

# ── Data Protection ──
DATA_PROTECTION_BANK=${DATA_PROTECTION_BANK}

# ── LLM ──
LLM_MODEL=${LLM_MODEL}

# ── CORS ──
CORS_1=${CORS_1}
CORS_2=${CORS_2}
CORS_3=${CORS_3}
CORS_4=${CORS_4}
CORS_5=${CORS_5}
CORS_6=${CORS_6}

# ── Google OAuth ──
GOOGLE_CLIENT_ID=${GOOGLE_CLIENT_ID}
GOOGLE_CLIENT_SECRET=${GOOGLE_CLIENT_SECRET}
GOOGLE_REDIRECT_URI=${GOOGLE_REDIRECT_URI}

# ── Twilio ──
TWILIO_SID=${TWILIO_SID}
TWILIO_TOKEN=${TWILIO_TOKEN}
TWILIO_PHONE=${TWILIO_PHONE}

# ── Relay ──
RELAY_BASE_URL=${RELAY_BASE_URL}
RELAY_START=${RELAY_START}
RELAY_END=${RELAY_END}
RELAY_WEEKENDS=${RELAY_WEEKENDS}
RELAY_JITTER=${RELAY_JITTER}

# ── Timer Service (shared between web + timer-service containers) ──
TIMER_API_KEY=${TIMER_API_KEY}
EOF

echo ""
echo "Environment file saved to $(pwd)/$ENV_FILE"
echo ""
echo "Next steps:"
echo "1. SSH to the VPS and run: sudo bash setup-env-vars.sh"
echo "2. In the repo folder run: docker compose down"
echo "3. Start again with: docker compose up -d --build"
echo "4. Check logs with: docker compose logs -f web"
echo ""
echo "The container now reads all values directly from $(pwd)/$ENV_FILE."
