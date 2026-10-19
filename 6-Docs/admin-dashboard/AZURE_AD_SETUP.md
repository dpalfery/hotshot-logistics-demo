# Azure AD Authentication Setup

This document explains how to configure Azure Active Directory (Azure AD) authentication for the Hotshot Logistics admin dashboard.

## New Automated Environment Variable Approach

**🚀 Enhanced Security & Deployment**: This project now uses an automated environment variable approach that eliminates manual `.env.local` management and prevents accidental secret commits. The build process automatically generates configuration from system environment variables.

**Key Benefits:**
- ✅ No manual `.env.local` file management required
- ✅ Prevents accidental commits of secrets
- ✅ Validates configuration at build time
- ✅ Consistent across development and production environments

## Prerequisites

- Azure subscription with access to Azure Active Directory
- Administrative access to create app registrations

## Azure AD App Registration Setup

### 1. Create App Registration

1. Go to the [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Click **New registration**
4. Enter the following details:
   - **Name**: `Hotshot Logistics Admin Dashboard`
   - **Supported account types**: `Accounts in this organizational directory only (Single tenant)`
   - **Redirect URI**: `http://localhost:3000` (for local development)
5. Click **Register**

### 2. Configure Authentication

1. In your app registration, go to **Authentication**
2. Under **Platform configurations**, click **Add a platform**
3. Select **Single-page application**
4. Add the following redirect URIs:
   - `http://localhost:3000` (for local development)
   - `https://localhost:3000` (for local development with HTTPS)
   - `https://your-domain.com` (for production - replace with your actual domain)
   - `https://admin.your-domain.com` (if using a subdomain for admin)
5. Under **Implicit grant and hybrid flows**, check:
   - `Access tokens (used for implicit flows)`
   - `ID tokens (used for implicit and hybrid flows)`
6. Under **Front-channel logout URL**, add:
   - `https://your-domain.com/login` (for production logout)
7. Click **Configure**

### 3. Add API Permissions

1. Go to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph** → **Delegated permissions**
4. Add the following permissions:
   - `User.Read` (to read user profile)
   - `openid` (for OpenID Connect)
   - `profile` (for user profile information)
5. Click **Add permissions**
6. Click **Grant admin consent** for your organization

### 4. Get Required Values

From your app registration overview page, note these values:

- **Application (client) ID**: This is your `NEXT_PUBLIC_AZURE_CLIENT_ID`
- **Directory (tenant) ID**: This is your `NEXT_PUBLIC_AZURE_TENANT_ID`

## Environment Configuration

### Automated Environment Variable Setup (Recommended)

The project now uses an automated approach for environment variable management that enhances security and deployment workflows.

#### 1. Set System Environment Variables

Set the following system environment variables with your Azure AD configuration:

```bash
# Windows Command Prompt
set NEXT_PUBLIC_AZURE_CLIENT_ID=your-actual-client-id-here
set NEXT_PUBLIC_AZURE_TENANT_ID=your-actual-tenant-id-here

# Windows PowerShell
$env:NEXT_PUBLIC_AZURE_CLIENT_ID="your-actual-client-id-here"
$env:NEXT_PUBLIC_AZURE_TENANT_ID="your-actual-tenant-id-here"

# Linux/macOS
export NEXT_PUBLIC_AZURE_CLIENT_ID="your-actual-client-id-here"
export NEXT_PUBLIC_AZURE_TENANT_ID="your-actual-tenant-id-here"
```

#### 2. Build Process (Automatic .env.local Generation)

When you run the build process, the system automatically generates `.env.local`:

```bash
npm run build
```

The `prebuild` script will automatically:
- Read your system environment variables
- Validate that required values are present and not placeholder values
- Generate `.env.local` with the correct configuration
- Display confirmation of successful generation

#### 3. Development Server

For development, you can run the development server directly:

```bash
npm run dev
```

The development server will use the existing `.env.local` file if it exists, or you can set environment variables in your development environment.

### Manual Setup (Alternative Method)

If you prefer manual configuration, you can still copy `.env.example` to `.env.local` and update the values manually:

```bash
cp .env.example .env.local
# Edit .env.local with your actual Azure AD values
```

## Troubleshooting

### Common Issues

1. **"AADSTS900144: The request body must contain the following parameter: 'client_id'"**
   - Ensure `NEXT_PUBLIC_AZURE_CLIENT_ID` environment variable is set correctly
   - Verify the client ID matches your Azure AD app registration
   - Run `npm run build` to regenerate `.env.local` if needed

2. **"AADSTS50011: The reply URL specified in the request does not match"**
   - Ensure redirect URIs in Azure AD match your application URLs
   - Add `http://localhost:3000` for local development

3. **Authentication fails silently**
   - Check browser console for detailed error messages
   - Verify API permissions are granted and consented

4. **Build fails with environment variable errors**
   - Ensure system environment variables are set before running `npm run build`
   - Check that values don't contain placeholder text like "your-actual-client-id-here"
   - Verify environment variables are available in your build environment (CI/CD, deployment pipeline)

5. **".env.local generation failed"**
   - Check that the `scripts/generate-env.js` file exists and is executable
   - Verify you have write permissions to create `.env.local`
   - Ensure required environment variables are set in your system

### Validation

The application will throw clear error messages if:
- Environment variables are missing or set to placeholder values
- Azure AD configuration is invalid
- The build process fails to generate `.env.local` properly

The `generate-env.js` script provides detailed error messages for common configuration issues.

## Production Deployment

For production deployment:

### 1. Azure AD App Registration Updates

1. **Update redirect URIs in Azure AD** to match your production domain:
   - Go to your app registration → **Authentication**
   - Add your production domain: `https://your-domain.com`
   - Add wildcard for subdomains if needed: `https://*.your-domain.com`
   - Remove any test/development URIs that shouldn't be used in production

2. **Configure logout URLs**:
   - Add front-channel logout URL: `https://your-domain.com/login`
   - This ensures proper logout redirection in production

3. **Review API permissions**:
   - Ensure all required permissions are granted
   - Grant admin consent for your organization
   - Verify that the permissions match your production requirements

### 2. Environment Variables Configuration

Set the following environment variables in your production environment:

```bash
# Azure AD Configuration
NEXT_PUBLIC_AZURE_CLIENT_ID=your-production-client-id
NEXT_PUBLIC_AZURE_TENANT_ID=your-production-tenant-id

# Production Domain
NEXT_PUBLIC_PRODUCTION_DOMAIN=your-actual-domain.com

# Node Environment
NODE_ENV=production
```

#### Deployment Platform Specific Configuration

**Azure App Service**:
```bash
# In Azure Portal: App Service → Configuration → Application settings
NEXT_PUBLIC_AZURE_CLIENT_ID = your-production-client-id
NEXT_PUBLIC_AZURE_TENANT_ID = your-production-tenant-id
NEXT_PUBLIC_PRODUCTION_DOMAIN = your-actual-domain.com
NODE_ENV = production
```

**Docker Deployment**:
```bash
# In your docker-compose.yml or deployment script
environment:
  - NEXT_PUBLIC_AZURE_CLIENT_ID=your-production-client-id
  - NEXT_PUBLIC_AZURE_TENANT_ID=your-production-tenant-id
  - NEXT_PUBLIC_PRODUCTION_DOMAIN=your-actual-domain.com
  - NODE_ENV=production
```

**CI/CD Pipeline**:
```yaml
# In your GitHub Actions or Azure DevOps pipeline
env:
  NEXT_PUBLIC_AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
  NEXT_PUBLIC_AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
  NEXT_PUBLIC_PRODUCTION_DOMAIN: ${{ secrets.PRODUCTION_DOMAIN }}
  NODE_ENV: production
```

### 3. HTTPS and SSL Configuration

The application is configured to enforce HTTPS in production:

1. **SSL Certificate**: Ensure your domain has a valid SSL certificate
2. **Load Balancer**: Configure your load balancer to forward HTTPS traffic
3. **Security Headers**: The application automatically adds security headers via Next.js config
4. **HSTS**: HTTP Strict Transport Security is enabled for enhanced security

### 4. Security Enhancements

#### Token Configuration
- **Access Token Lifetime**: Configure appropriate lifetime in Azure AD (default 1 hour)
- **Refresh Token Lifetime**: Set based on your security requirements
- **Session Management**: The app uses sessionStorage with secure cookies

#### Monitoring and Logging
- Enable Azure AD sign-in logs for security monitoring
- Monitor authentication failures and suspicious activities
- Set up alerts for multiple failed login attempts

### 5. Domain and DNS Configuration

1. **Custom Domain**: Configure your custom domain in your hosting platform
2. **DNS Records**: Add appropriate DNS records for your domain
3. **CDN Configuration**: If using a CDN, ensure it forwards necessary headers

### 6. Testing Production Configuration

Before going live:

1. **Test HTTPS enforcement**: Verify all HTTP requests are redirected to HTTPS
2. **Test authentication flow**: Complete login/logout cycle with production URLs
3. **Test API connectivity**: Ensure API calls work with production authentication
4. **Test security headers**: Verify all security headers are present
5. **Test mobile responsiveness**: Ensure authentication works on mobile devices

### 7. Post-Deployment Verification

After deployment:

1. **Monitor authentication logs** in Azure AD for any issues
2. **Verify SSL certificate** is valid and properly configured
3. **Test user authentication** with real user accounts
4. **Check security headers** using browser developer tools
5. **Monitor performance** and response times for authentication flows

### Deployment Platforms

#### Azure App Service
```bash
# In Azure Portal: App Service → Configuration → Application settings
# Add these settings:
NEXT_PUBLIC_AZURE_CLIENT_ID = your-production-client-id
NEXT_PUBLIC_AZURE_TENANT_ID = your-production-tenant-id
```

#### Docker Deployment
```bash
# In your docker-compose.yml or deployment script
environment:
  - NEXT_PUBLIC_AZURE_CLIENT_ID=your-production-client-id
  - NEXT_PUBLIC_AZURE_TENANT_ID=your-production-tenant-id
```

#### CI/CD Pipeline
```yaml
# In your GitHub Actions or Azure DevOps pipeline
env:
  NEXT_PUBLIC_AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
  NEXT_PUBLIC_AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
```

The build process will automatically generate `.env.local` with these values during deployment.

## Security Notes

### Enhanced Security with Automated Approach

The automated environment variable approach provides several security benefits:

- **No Manual .env.local Management**: Eliminates risk of accidentally committing secrets
- **System Environment Variables**: More secure than file-based configuration
- **Build-Time Validation**: Script validates that placeholder values aren't used
- **Automatic Generation**: Ensures consistent configuration across environments

### General Security Best Practices

- **Never commit `.env.local` to version control** (automatically prevented by `.gitignore`)
- **Use different Azure AD apps for development and production**
- **Set environment variables securely in your deployment environment**
- **Regularly rotate client secrets** (if using server-side apps)
- **Monitor authentication logs in Azure AD**
- **Implement proper logout functionality to clear tokens**
- **Use HTTPS in production** (enforced by Next.js in production builds)

## Additional Resources

- [Azure AD App Registration Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [MSAL.js Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-js-initializing-client-applications)
- [Single-page Application Authentication](https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-spa-overview)