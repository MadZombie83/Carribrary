plugins { id("com.android.application"); id("org.jetbrains.kotlin.android"); id("org.jetbrains.kotlin.plugin.compose") }
android {
 namespace="com.household.homelibrary"; compileSdk=36
 defaultConfig { applicationId="com.household.homelibrary"; minSdk=29; targetSdk=36; versionCode=20036; versionName="2.0.36-preview" }
 buildFeatures { compose=true; buildConfig=true }
 compileOptions { sourceCompatibility=JavaVersion.VERSION_17; targetCompatibility=JavaVersion.VERSION_17 }
 kotlinOptions { jvmTarget="17" }
 buildTypes { release { isMinifyEnabled=false } }
}
dependencies {
 implementation("com.google.zxing:core:3.5.3")
 implementation(platform("androidx.compose:compose-bom:2025.04.01"))
 implementation("androidx.activity:activity-compose:1.10.1")
 implementation("androidx.compose.material3:material3")
 implementation("androidx.compose.ui:ui-tooling-preview")
 implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.9.0")
 implementation("androidx.lifecycle:lifecycle-runtime-compose:2.9.0")
 implementation("androidx.camera:camera-camera2:1.4.2")
 implementation("androidx.camera:camera-lifecycle:1.4.2")
 implementation("androidx.camera:camera-view:1.4.2")
 implementation("com.google.mlkit:barcode-scanning:17.3.0")
 implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.10.2")
}
