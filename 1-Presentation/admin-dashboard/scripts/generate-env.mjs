#!/usr/bin/env node

/**
 * Generate .env.local file from system environment variables
 * This script reads Azure AD configuration from system environment variables
 * and generates the .env.local file automatically for secure deployments.
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));

function generateEnvFile() {
  const envFilePath = path.join(process.cwd(), '.env.local');
  let azureClientId = process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;
  let azureTenantId = process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

  // If environment variables are missing but .env.local exists, keep existing file
  if ((!azureClientId || !azureTenantId) && fs.existsSync(envFilePath)) {
    const existing = fs.readFileSync(envFilePath, 'utf8');
    if (existing.includes('NEXT_PUBLIC_AZURE_CLIENT_ID=') && existing.includes('NEXT_PUBLIC_AZURE_TENANT_ID=')) {
      console.log('✓ Using existing .env.local configuration.');
      return;
    }
  }

  // Validate required environment variables
  if (!azureClientId || !azureTenantId) {
    console.error('❌ Missing required environment variables:');
    if (!azureClientId) console.error('   - NEXT_PUBLIC_AZURE_CLIENT_ID');
    if (!azureTenantId) console.error('   - NEXT_PUBLIC_AZURE_TENANT_ID');
    console.error('\nPlease set these environment variables before running the build.');
    console.error('Example:');
    console.error('  export NEXT_PUBLIC_AZURE_CLIENT_ID="your-client-id"');
    console.error('  export NEXT_PUBLIC_AZURE_TENANT_ID="your-tenant-id"');
    process.exit(1);
  }

  // Validate that we're not using placeholder values
  if (azureClientId.includes('your-') || azureTenantId.includes('your-')) {
    console.error('❌ Environment variables still contain placeholder values.');
    console.error('Please replace placeholder values with actual Azure AD configuration.');
    process.exit(1);
  }

  // Determine environment and set appropriate URLs
  const isProduction = process.env.NODE_ENV === 'production';
  const productionDomain = process.env.NEXT_PUBLIC_PRODUCTION_DOMAIN || 'your-domain.com';
  const apiBaseUrl = isProduction
    ? `https://${productionDomain}/api`
    : 'https://localhost:5001/api';

  // Generate .env.local content with production-ready configuration
  const envContent = `# API Configuration
NEXT_PUBLIC_API_BASE_URL=${apiBaseUrl}

# Azure AD Configuration (Auto-generated from system environment variables)
# These values are automatically populated from your system environment variables
NEXT_PUBLIC_AZURE_CLIENT_ID=${azureClientId}
NEXT_PUBLIC_AZURE_TENANT_ID=${azureTenantId}

# Production Configuration
NODE_ENV=${isProduction ? 'production' : 'development'}
NEXT_PUBLIC_PRODUCTION_DOMAIN=${productionDomain}

# Security Configuration
# HTTPS is enforced in production via Next.js config and middleware

# Note: This file is auto-generated. Do not edit manually.
# Set NEXT_PUBLIC_AZURE_CLIENT_ID and NEXT_PUBLIC_AZURE_TENANT_ID as system environment variables instead.
# For production deployment, also set NEXT_PUBLIC_PRODUCTION_DOMAIN to your actual domain.
`;

  const envPath = path.join(scriptDir, '..', '.env.local');

  try {
    // Write .env.local file
    fs.writeFileSync(envPath, envContent, 'utf8');
    console.log('✅ Successfully generated .env.local file');
    console.log(`   📄 File location: ${envPath}`);
    console.log(`   🔑 Azure Client ID: ${azureClientId.substring(0, 8)}...`);
    console.log(`   🏢 Azure Tenant ID: ${azureTenantId.substring(0, 8)}...`);
  } catch (error) {
    console.error('❌ Failed to write .env.local file:', error.message);
    process.exit(1);
  }
}

// Run the script
generateEnvFile();