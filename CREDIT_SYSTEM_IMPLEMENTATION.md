# Process Zero 2.0 - Credit System Implementation

## Overview
This document describes the credit-based access system implemented for Process Zero 2.0, a pay-to-use sales opportunity platform that connects qualified sales representatives with high-quality booked demo appointments.

## Architecture

### Core Entities

#### 1. UserWallet
- **Location**: `ProcessZero.Domain/Entities/UserWallet.cs`
- **Purpose**: Stores each user's credit balance and lifetime statistics
- **Key Properties**:
  - `CreditBalance` - Current available credits
  - `TotalCreditsPurchased` - Lifetime total credits purchased
  - `TotalCreditsConsumed` - Lifetime total credits consumed
  - `SubscriptionId` - Optional subscription reference
  - `SubscriptionStatus` - Status of recurring credits

#### 2. CreditTransaction
- **Location**: `ProcessZero.Domain/Entities/CreditTransaction.cs`
- **Purpose**: Tracks all credit transactions (purchases, consumption, adjustments)
- **Transaction Types**:
  - `Purchase` - Credits purchased via packages
  - `Consumption` - Credits consumed for platform access
  - `Refund` - Credits refunded
  - `Adjustment` - Manual admin adjustments
  - `Bonus` - Bonus credits awarded
  - `Subscription` - Recurring credit allocation

#### 3. CreditPackage
- **Location**: `ProcessZero.Domain/Entities/CreditPackage.cs`
- **Purpose**: Defines purchaseable credit packages
- **Key Properties**:
  - `CreditAmount` - Number of credits in package
  - `Price` - Cost in configured currency
  - `DurationMinutes` - Time duration credits provide
  - `IsActive` - Package availability status
  - `IsSubscription` - Whether package is recurring

### Service Layer

#### IUserWalletService
- **Location**: `ProcessZero.Application/Interfaces/IUserWalletService.cs`
- **Methods**:
  - `GetUserWalletAsync(userId)` - Get or create user wallet
  - `GetAvailablePackagesAsync()` - List active packages
  - `PurchaseCreditsAsync(userId, request)` - Purchase credits
  - `ConsumeCreditsAsync(userId, request)` - Consume credits
  - `CheckCreditBalanceAsync(userId, requiredCredits)` - Check balance
  - `GetTransactionHistoryAsync(userId, page, pageSize)` - Transaction history
  - `AdjustCreditsAsync(userId, amount, reason)` - Admin adjustment

### API Endpoints

#### CreditController
- **Location**: `Controllers/CreditController.cs`
- **Base Path**: `/api/credit`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/wallet` | Required | Get current user's wallet |
| GET | `/packages` | Anonymous | Get available packages |
| GET | `/packages/all` | Admin | Get all packages |
| POST | `/packages` | Admin | Create new package |
| POST | `/purchase` | Required | Purchase credits |
| POST | `/consume` | Required | Consume credits |
| POST | `/check` | Required | Check credit balance |
| GET | `/transactions` | Required | Get transaction history |
| GET | `/transactions/{id}` | Required | Get specific transaction |
| POST | `/adjust?userId=...` | Admin | Adjust user credits |
| POST | `/initialize` | Required | Initialize user wallet |

## Database

### Migration
- **File**: `ProcessZero.Domain/Migrations/20260721_CreateCreditSystem.cs`
- **Tables Created**:
  - `CreditPackages`
  - `UserWallets` (with unique constraint on UserId)
  - `CreditTransactions`

### Indexes
- `IX_UserWallets_UserId` - Unique index for wallet lookups
- `IX_CreditTransactions_UserWalletId` - Foreign key index
- `IX_CreditTransactions_TransactionDate` - Date-based queries
- `IX_CreditTransactions_UserWalletId_TransactionDate` - Composite for user history
- `IX_CreditPackages_IsActive` - Package availability queries
- `IX_CreditPackages_SortOrder` - UI ordering

## Usage Flow

1. **User Registration**: Wallet is automatically created when user first accesses credit system
2. **Browse Packages**: Users can view available packages at `/api/credit/packages`
3. **Purchase Credits**: User selects package and purchases via `/api/credit/purchase`
4. **Consume Credits**: Credits are consumed for platform access via `/api/credit/consume`
5. **Track History**: All transactions are recorded and queryable via `/api/credit/transactions`

## Integration Points

The credit system is designed to integrate with existing Process Zero features:
- **Meetings**: Can consume credits when scheduling demos
- **Products**: Credit packages can be linked to product access
- **Invoices**: Purchases generate transactions for billing integration

## Next Steps

1. Add initial seed data for credit packages
2. Implement credit consumption middleware for protected endpoints
3. Add integration with payment providers (Stripe, PayPal)
4. Create admin UI for package management
5. Add subscription management for recurring credits