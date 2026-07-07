#!/bin/bash
# ProcessZero - Setup Docker environment variables for a Linux VPS

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.processzero.env"

echo "============================================================"
echo "ProcessZero VPS environment setup"
echo "This writes one Docker env file used by docker-compose.yml"
echo "============================================================"
echo ""

if [ "$EUID" -ne 0 ]; then
   echo "Run with sudo: sudo bash setup-env-vars.sh"
   exit 1
fi

prompt_value() {
   local var_name="$1"
   local prompt_text="$2"
   local default_value="${3-}"
   local current_value="${4-}"
   local entered_value=""
   local effective_default=""

   if [ -n "$current_value" ]; then
      effective_default="$current_value"
   else
      effective_default="$default_value"
   fi

   if [ -n "$effective_default" ]; then
      read -r -p "$prompt_text [$effective_default]: " entered_value
      if [ -z "$entered_value" ]; then
         entered_value="$effective_default"
      fi
   else
      read -r -p "$prompt_text: " entered_value
   fi

   printf -v "$var_name" '%s' "$entered_value"
}

escape_env_value() {
   printf '%s' "$1" | tr -d '\r' | tr '\n' ' '
}

get_existing_value() {
   local key="$1"

   if [ ! -f "$ENV_FILE" ]; then
      return 0
   fi

   grep -m1 "^${key}=" "$ENV_FILE" | cut -d '=' -f 2-
}

existing_connection_string="$(get_existing_value "ConnectionStrings__DefaultConnection")"
existing_jwt_key="$(get_existing_value "Jwt__Key")"
existing_jwt_issuer="$(get_existing_value "Jwt__Issuer")"
existing_jwt_audience="$(get_existing_value "Jwt__Audience")"
existing_cal_base_url="$(get_existing_value "CalOptions__BaseUrl")"
existing_cal_api_key="$(get_existing_value "CalOptions__ApiKey")"
existing_paystack_secret_key="$(get_existing_value "Paystack__SecretKey")"
existing_payfast_merchant_id="$(get_existing_value "PayFast__MerchantId")"
existing_payfast_merchant_key="$(get_existing_value "PayFast__MerchantKey")"
existing_payfast_passphrase="$(get_existing_value "PayFast__Passphrase")"
existing_payfast_use_sandbox="$(get_existing_value "PayFast__UseSandbox")"
existing_assessment_pass_mark="$(get_existing_value "Assessment__PassMark")"
existing_payroll_commission_rate="$(get_existing_value "Payroll__CommissionRate")"
existing_data_protection_purpose="$(get_existing_value "DataProtection__Purposes__BankAccount")"
existing_llm_model="$(get_existing_value "LLM__Model")"
existing_cors_0="$(get_existing_value "Cors__AllowedOrigins__0")"
existing_cors_1="$(get_existing_value "Cors__AllowedOrigins__1")"
existing_cors_2="$(get_existing_value "Cors__AllowedOrigins__2")"
existing_cors_3="$(get_existing_value "Cors__AllowedOrigins__3")"
existing_cors_4="$(get_existing_value "Cors__AllowedOrigins__4")"
existing_cors_5="$(get_existing_value "Cors__AllowedOrigins__5")"
existing_google_client_id="$(get_existing_value "GoogleOAuth__ClientId")"
existing_google_client_secret="$(get_existing_value "GoogleOAuth__ClientSecret")"
existing_google_redirect_uri="$(get_existing_value "GoogleOAuth__RedirectUri")"
existing_twilio_account_sid="$(get_existing_value "Twilio__AccountSid")"
existing_twilio_auth_token="$(get_existing_value "Twilio__AuthToken")"
existing_twilio_phone_number="$(get_existing_value "Twilio__PhoneNumber")"
existing_relay_public_base_url="$(get_existing_value "Relay__PublicBaseUrl")"
existing_relay_start_hour="$(get_existing_value "Relay__SendWindowStartHour")"
existing_relay_end_hour="$(get_existing_value "Relay__SendWindowEndHour")"
existing_relay_weekends="$(get_existing_value "Relay__SendOnWeekends")"
existing_relay_skip_percent="$(get_existing_value "Relay__SendJitterSkipPercent")"

echo "Enter the values once. Press Enter to keep the current/default value."
echo ""

prompt_value connection_string "Database connection string" "Server=46.202.170.203;Port=3306;Database=processzero;User=xhanti;Password=change_me;" "$existing_connection_string"
prompt_value jwt_key "JWT key" "change_me_minimum_32_characters_long" "$existing_jwt_key"
prompt_value jwt_issuer "JWT issuer" "ProcessZero" "$existing_jwt_issuer"
prompt_value jwt_audience "JWT audience" "ProcessZeroUsers" "$existing_jwt_audience"
prompt_value cal_base_url "Cal.com base URL" "https://api.cal.com/v2" "$existing_cal_base_url"
prompt_value cal_api_key "Cal.com API key" "" "$existing_cal_api_key"
prompt_value paystack_secret_key "Paystack secret key" "" "$existing_paystack_secret_key"
prompt_value payfast_merchant_id "PayFast merchant ID" "" "$existing_payfast_merchant_id"
prompt_value payfast_merchant_key "PayFast merchant key" "" "$existing_payfast_merchant_key"
prompt_value payfast_passphrase "PayFast passphrase" "" "$existing_payfast_passphrase"
prompt_value payfast_use_sandbox "PayFast use sandbox (true/false)" "true" "$existing_payfast_use_sandbox"
prompt_value assessment_pass_mark "Assessment pass mark" "70.0" "$existing_assessment_pass_mark"
prompt_value payroll_commission_rate "Payroll commission rate" "0.20" "$existing_payroll_commission_rate"
prompt_value data_protection_purpose "Data protection purpose for bank account" "ProcessZero.BankAccountService.V1" "$existing_data_protection_purpose"
prompt_value llm_model "LLM model" "llama3:latest" "$existing_llm_model"
prompt_value cors_0 "CORS origin 1" "http://localhost:3000" "$existing_cors_0"
prompt_value cors_1 "CORS origin 2" "http://localhost:5173" "$existing_cors_1"
prompt_value cors_2 "CORS origin 3" "http://77.93.155.211" "$existing_cors_2"
prompt_value cors_3 "CORS origin 4" "https://77.93.155.211" "$existing_cors_3"
prompt_value cors_4 "CORS origin 5" "https://processzero.xyz" "$existing_cors_4"
prompt_value cors_5 "CORS origin 6" "https://www.processzero.xyz" "$existing_cors_5"
prompt_value google_client_id "Google OAuth client ID" "" "$existing_google_client_id"
prompt_value google_client_secret "Google OAuth client secret" "" "$existing_google_client_secret"
prompt_value google_redirect_uri "Google OAuth redirect URI" "https://localhost:7183/api/googleauth/callback" "$existing_google_redirect_uri"
prompt_value twilio_account_sid "Twilio account SID" "" "$existing_twilio_account_sid"
prompt_value twilio_auth_token "Twilio auth token" "" "$existing_twilio_auth_token"
prompt_value twilio_phone_number "Twilio phone number" "" "$existing_twilio_phone_number"
prompt_value relay_public_base_url "Relay public base URL" "https://api.processzero.xyz" "$existing_relay_public_base_url"
prompt_value relay_start_hour "Relay send window start hour" "0" "$existing_relay_start_hour"
prompt_value relay_end_hour "Relay send window end hour" "24" "$existing_relay_end_hour"
prompt_value relay_weekends "Relay send on weekends (true/false)" "true" "$existing_relay_weekends"
prompt_value relay_skip_percent "Relay jitter skip percent" "0" "$existing_relay_skip_percent"

tmp_file="$(mktemp)"

cat > "$tmp_file" <<EOF
ConnectionStrings__DefaultConnection=$(escape_env_value "$connection_string")
Jwt__Key=$(escape_env_value "$jwt_key")
Jwt__Issuer=$(escape_env_value "$jwt_issuer")
Jwt__Audience=$(escape_env_value "$jwt_audience")
CalOptions__BaseUrl=$(escape_env_value "$cal_base_url")
CalOptions__ApiKey=$(escape_env_value "$cal_api_key")
Paystack__SecretKey=$(escape_env_value "$paystack_secret_key")
PayFast__MerchantId=$(escape_env_value "$payfast_merchant_id")
PayFast__MerchantKey=$(escape_env_value "$payfast_merchant_key")
PayFast__Passphrase=$(escape_env_value "$payfast_passphrase")
PayFast__UseSandbox=$(escape_env_value "$payfast_use_sandbox")
Assessment__PassMark=$(escape_env_value "$assessment_pass_mark")
Payroll__CommissionRate=$(escape_env_value "$payroll_commission_rate")
DataProtection__Purposes__BankAccount=$(escape_env_value "$data_protection_purpose")
LLM__Model=$(escape_env_value "$llm_model")
Cors__AllowedOrigins__0=$(escape_env_value "$cors_0")
Cors__AllowedOrigins__1=$(escape_env_value "$cors_1")
Cors__AllowedOrigins__2=$(escape_env_value "$cors_2")
Cors__AllowedOrigins__3=$(escape_env_value "$cors_3")
Cors__AllowedOrigins__4=$(escape_env_value "$cors_4")
Cors__AllowedOrigins__5=$(escape_env_value "$cors_5")
GoogleOAuth__ClientId=$(escape_env_value "$google_client_id")
GoogleOAuth__ClientSecret=$(escape_env_value "$google_client_secret")
GoogleOAuth__RedirectUri=$(escape_env_value "$google_redirect_uri")
Twilio__AccountSid=$(escape_env_value "$twilio_account_sid")
Twilio__AuthToken=$(escape_env_value "$twilio_auth_token")
Twilio__PhoneNumber=$(escape_env_value "$twilio_phone_number")
Relay__PublicBaseUrl=$(escape_env_value "$relay_public_base_url")
Relay__SendWindowStartHour=$(escape_env_value "$relay_start_hour")
Relay__SendWindowEndHour=$(escape_env_value "$relay_end_hour")
Relay__SendOnWeekends=$(escape_env_value "$relay_weekends")
Relay__SendJitterSkipPercent=$(escape_env_value "$relay_skip_percent")
EOF

install -m 600 "$tmp_file" "$ENV_FILE"
rm -f "$tmp_file"

echo ""
echo "Environment file saved to $ENV_FILE"
echo ""
echo "Next steps:"
echo "1. SSH to the VPS and run: sudo bash setup-env-vars.sh"
echo "2. In the repo folder run: docker compose down"
echo "3. Start again with: docker compose up -d --build"
echo "4. Check logs with: docker compose logs -f web"
echo ""
echo "The container now reads all values directly from $ENV_FILE."
