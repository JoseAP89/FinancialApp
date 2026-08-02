# Build.ps1 - Complete working build script for Android Release with Manual Signing
Write-Host "🚀 Starting Android Release Build..." -ForegroundColor Green

# ============================================
# 1. ENVIRONMENT SETUP
# ============================================

# Use external SSD for temp files
$env:TEMP = "E:\tmp\Android\BuildTemp"
$env:TMP = "E:\tmp\Android\BuildTemp"
$env:NUGET_PACKAGES = "E:\tmp\Android\NuGetPackages"
$env:MSBUILD_TEMP = "E:\tmp\Android\BuildTemp\msbuild"

# Define log file paths
$BUILD_LOG = "E:\tmp\Android\Log\build.log"
$PUBLISH_LOG = "E:\tmp\Android\Log\publish.log"
$ANDROID_LOG_DIR = "E:\tmp\Android\Log"

# Keystore settings
$KEYSTORE_PATH = "myapp.keystore"
$KEYSTORE_ALIAS = "myapp"
$KEYSTORE_PASS = "J1o2s3e4"
$KEY_ALIAS = "myapp"

# Android SDK paths
$APKSIGNER_PATH = "C:\Program Files (x86)\Android\android-sdk\build-tools\36.0.0\lib\apksigner.jar"
$ZIPALIGN_PATH = "C:\Program Files (x86)\Android\android-sdk\build-tools\36.0.0\zipalign.exe"

# Java memory settings
$env:JAVA_OPTS = "-Xmx2G"

# Force single-process build to reduce memory
$env:MSBUILD_SINGLEPROCESS = "1"

# Create directories on external SSD
Write-Host "📁 Creating temp directories on external SSD..." -ForegroundColor Yellow
mkdir "E:\tmp\Android\BuildTemp" -Force | Out-Null
mkdir "E:\tmp\Android\NuGetPackages" -Force | Out-Null
mkdir "E:\tmp\Android\BuildTemp\msbuild" -Force | Out-Null
mkdir $ANDROID_LOG_DIR -Force | Out-Null

# ============================================
# 2. CHECK MEMORY
# ============================================

Write-Host "📊 Checking system resources..." -ForegroundColor Yellow
$os = Get-WmiObject -Class Win32_OperatingSystem
$freeRAM = [math]::Round($os.FreePhysicalMemory / 1MB, 1)
$totalRAM = [math]::Round($os.TotalVisibleMemorySize / 1MB, 0)
Write-Host "   RAM: $freeRAM GB free / $totalRAM GB total" -ForegroundColor Cyan

if ($freeRAM -lt 4) {
    Write-Host "⚠️  WARNING: Low RAM ($freeRAM GB free). Close other applications!" -ForegroundColor Red
    Read-Host "Press Enter to continue anyway, or Ctrl+C to cancel"
}

# ============================================
# 3. CLEAN
# ============================================

Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue

# ============================================
# 4. BUILD RELEASE (WITH REDUCED MEMORY USAGE)
# ============================================

Write-Host "🔨 Building Release (this will take 5-10 minutes)..." -ForegroundColor Yellow

# Build with full verbose output to see the actual error
$buildOutput = dotnet build -f net10.0-android -c Release -m:1 `
    -p:AndroidEnableProfiledAot=false `
    -p:AndroidLinkMode=SdkOnly `
    -p:JavaMaximumHeapSize=2G `
    -p:AndroidEnableMultiDex=true `
    -v:normal 2>&1

# Save build output to a file for debugging
$buildOutput | Out-File -FilePath $BUILD_LOG -Encoding UTF8
Write-Host "📄 Build log saved to: $BUILD_LOG" -ForegroundColor Cyan

# Check if build succeeded
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build succeeded!" -ForegroundColor Green
    
    # ============================================
    # 5. PUBLISH (UNSIGNED APK)
    # ============================================
    
    Write-Host "📦 Publishing unsigned APK..." -ForegroundColor Yellow
    
    $publishOutput = dotnet publish -f net10.0-android -c Release -m:1 `
        -p:AndroidEnableProfiledAot=false `
        -p:AndroidLinkMode=SdkOnly `
        -p:JavaMaximumHeapSize=2G `
        -p:AndroidEnableMultiDex=true `
        -p:AndroidPackageFormat=apk `
        -v:normal 2>&1
    
    # Save publish output to a file for debugging
    $publishOutput | Out-File -FilePath $PUBLISH_LOG -Encoding UTF8
    Write-Host "📄 Publish log saved to: $PUBLISH_LOG" -ForegroundColor Cyan
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Publish succeeded!" -ForegroundColor Green
        
        # ============================================
        # 6. SIGN THE APK MANUALLY
        # ============================================
        
        Write-Host "🔐 Signing APK with apksigner..." -ForegroundColor Yellow
        
        # Navigate to the publish folder
        Set-Location bin\Release\net10.0-android\publish\
        
        # Sign the APK using apksigner
        $signOutput = java -jar $APKSIGNER_PATH sign `
            --ks ..\..\..\..\$KEYSTORE_PATH `
            --ks-pass pass:$KEYSTORE_PASS `
            --ks-key-alias $KEY_ALIAS `
            --key-pass pass:$KEYSTORE_PASS `
            --min-sdk-version 24 `
            com.companyname.financialapp.apk 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ APK signed successfully!" -ForegroundColor Green
            
            # ============================================
            # 7. VERIFY THE SIGNATURE
            # ============================================
            
            Write-Host "🔍 Verifying signature..." -ForegroundColor Yellow
            $verifyOutput = java -jar $APKSIGNER_PATH verify -v com.companyname.financialapp.apk 2>&1
            $verifyOutput | Out-File -FilePath "$ANDROID_LOG_DIR\verify.log" -Encoding UTF8
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Signature verified successfully!" -ForegroundColor Green
                
                # ============================================
                # 8. INSTALL ON PHONE (OPTIONAL)
                # ============================================
                
                Write-Host ""
                Write-Host "📱 APK Location:" -ForegroundColor Cyan
                Write-Host "   $PWD\com.companyname.financialapp.apk" -ForegroundColor White
                Write-Host ""
                Write-Host "📋 To install on your phone:" -ForegroundColor Cyan
                Write-Host "   adb install -r com.companyname.financialapp.apk" -ForegroundColor White
                Write-Host ""
                
                # Ask if user wants to install now
                $installNow = Read-Host "Do you want to install the APK on your phone now? (y/n)"
                if ($installNow -eq 'y' -or $installNow -eq 'Y') {
                    Write-Host "📱 Installing on phone..." -ForegroundColor Yellow
                    adb install -r com.companyname.financialapp.apk
                    
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "✅ App installed successfully!" -ForegroundColor Green
                    } else {
                        Write-Host "❌ Installation failed!" -ForegroundColor Red
                        Write-Host "Make sure your phone is connected and USB Debugging is enabled." -ForegroundColor Yellow
                    }
                }
                
            } else {
                Write-Host "❌ Signature verification failed!" -ForegroundColor Red
                Write-Host "Check $ANDROID_LOG_DIR\verify.log for details" -ForegroundColor Yellow
            }
            
        } else {
            Write-Host "❌ APK signing failed!" -ForegroundColor Red
            Write-Host $signOutput -ForegroundColor Red
        }
        
        # Return to the project root
        Set-Location ..\..\..\
        
    } else {
        Write-Host "❌ Publish failed!" -ForegroundColor Red
        Write-Host "Check $PUBLISH_LOG for details" -ForegroundColor Yellow
    }
    
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "📄 Check $BUILD_LOG for full error details" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "💡 Common issues and solutions:" -ForegroundColor Yellow
    Write-Host "   1. Not enough RAM - Close all other applications" -ForegroundColor White
    Write-Host "   2. Antivirus blocking Java - Temporarily disable antivirus" -ForegroundColor White
    Write-Host "   3. Corrupted build cache - Delete bin/obj folders and try again" -ForegroundColor White
    Write-Host "   4. Try Debug build: dotnet build -f net10.0-android -c Debug" -ForegroundColor White
    Write-Host ""
    Write-Host "🔍 To see the last 20 lines of the build log:" -ForegroundColor Yellow
    Write-Host "   Get-Content $BUILD_LOG -Tail 20" -ForegroundColor White
}