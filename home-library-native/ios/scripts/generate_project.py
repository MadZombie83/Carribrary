"""Regenerate the checked-in Xcode project. Python stdlib only; no XcodeGen."""
from pathlib import Path
import hashlib, json, plistlib
import xml.etree.ElementTree as ET
root = Path(__file__).resolve().parents[1]
project = root / 'HomeLibrary.xcodeproj'
project.mkdir(exist_ok=True)
def ident(name): return hashlib.sha256(name.encode()).hexdigest()[:24].upper()
objects = []
def obj(name, body):
    objects.append(f'{ident(name)} = {{ {body} }};')
    return ident(name)
def array(values): return '(' + ', '.join(values) + ',)'
files = sorted((root/'HomeLibrary').glob('*.swift'))
testfiles = sorted((root/'HomeLibraryTests').glob('*.swift'))
apprefs=[]; tests=[]; sourcebuild=[]; testbuild=[]; resources=[]
for f in files + testfiles:
    relative=f.relative_to(root).as_posix()
    ref=obj(relative, f'isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = "{f.name}"; sourceTree = "<group>";')
    build=obj('build:'+relative, f'isa = PBXBuildFile; fileRef = {ref};')
    if f in files: apprefs.append(ref); sourcebuild.append(build)
    else: tests.append(ref); testbuild.append(build)
for name,typ in [('Assets.xcassets','folder.assetcatalog'),('PrivacyInfo.xcprivacy','text.xml'),('help.json','text.json')]:
    ref=obj(name,f'isa = PBXFileReference; lastKnownFileType = {typ}; path = {name}; sourceTree = "<group>";')
    apprefs.append(ref); resources.append(obj('build:'+name,f'isa = PBXBuildFile; fileRef = {ref};'))
apprefs.append(obj('plist','isa = PBXFileReference; lastKnownFileType = text.plist.xml; path = Info.plist; sourceTree = "<group>";'))
app=obj('product-app','isa = PBXFileReference; explicitFileType = wrapper.application; path = HomeLibrary.app; sourceTree = BUILT_PRODUCTS_DIR;')
test=obj('product-tests','isa = PBXFileReference; explicitFileType = wrapper.cfbundle; path = HomeLibraryTests.xctest; sourceTree = BUILT_PRODUCTS_DIR;')
groupapp=obj('group-app',f'isa = PBXGroup; children = {array(apprefs)}; path = HomeLibrary; sourceTree = "<group>";')
grouptest=obj('group-tests',f'isa = PBXGroup; children = {array(tests)}; path = HomeLibraryTests; sourceTree = "<group>";')
products=obj('products',f'isa = PBXGroup; children = {array([app,test])}; name = Products; sourceTree = "<group>";')
main=obj('main',f'isa = PBXGroup; children = {array([groupapp,grouptest,products])}; sourceTree = "<group>";')
def phase(name,isa,refs): return obj(name, f'isa = {isa}; buildActionMask = 2147483647; files = {array(refs) if refs else "()"}; runOnlyForDeploymentPostprocessing = 0;')
app_phases=[phase('app-sources','PBXSourcesBuildPhase',sourcebuild),phase('app-frameworks','PBXFrameworksBuildPhase',[]),phase('app-resources','PBXResourcesBuildPhase',resources)]
test_phases=[phase('test-sources','PBXSourcesBuildPhase',testbuild),phase('test-frameworks','PBXFrameworksBuildPhase',[]),phase('test-resources','PBXResourcesBuildPhase',[])]
def configs(name,settings):
    refs=[]
    for mode in ['Debug','Release']:
        opts=dict(settings)
        opts['SWIFT_OPTIMIZATION_LEVEL']='"-Onone"' if mode=='Debug' else '"-O"'
        if mode=='Debug': opts['ENABLE_TESTABILITY']='YES'; opts['SWIFT_ACTIVE_COMPILATION_CONDITIONS']='DEBUG'
        refs.append(obj(name+'-'+mode,'isa = XCBuildConfiguration; buildSettings = { '+' '.join(f'{k} = {v};' for k,v in opts.items())+' }; name = '+mode+';'))
    return obj(name+'-configs',f'isa = XCConfigurationList; buildConfigurations = {array(refs)}; defaultConfigurationIsVisible = 0; defaultConfigurationName = Release;')
common={'SDKROOT':'iphoneos','IPHONEOS_DEPLOYMENT_TARGET':'17.0','SWIFT_VERSION':'5.0','CLANG_ENABLE_MODULES':'YES','CLANG_ENABLE_OBJC_ARC':'YES','SWIFT_STRICT_CONCURRENCY':'minimal','ENABLE_USER_SCRIPT_SANDBOXING':'YES'}
projconfig=configs('project',common)
appconfig=configs('app',{'PRODUCT_BUNDLE_IDENTIFIER':'com.household.HomeLibrary.ios','PRODUCT_NAME':'"$(TARGET_NAME)"','INFOPLIST_FILE':'HomeLibrary/Info.plist','CODE_SIGN_STYLE':'Automatic','DEVELOPMENT_TEAM':'""','TARGETED_DEVICE_FAMILY':'"1,2"','ASSETCATALOG_COMPILER_APPICON_NAME':'AppIcon','MARKETING_VERSION':'0.1.28','CURRENT_PROJECT_VERSION':'29','GENERATE_INFOPLIST_FILE':'NO'})
testconfig=configs('tests',{'PRODUCT_BUNDLE_IDENTIFIER':'com.household.HomeLibrary.ios.tests','PRODUCT_NAME':'"$(TARGET_NAME)"','GENERATE_INFOPLIST_FILE':'YES','CODE_SIGN_STYLE':'Automatic','DEVELOPMENT_TEAM':'""','TARGETED_DEVICE_FAMILY':'"1,2"','TEST_HOST':'"$(BUILT_PRODUCTS_DIR)/HomeLibrary.app/$(BUNDLE_EXECUTABLE_FOLDER_PATH)/HomeLibrary"','BUNDLE_LOADER':'"$(TEST_HOST)"'})
proxy=obj('proxy',f'isa = PBXContainerItemProxy; containerPortal = {ident("project")}; proxyType = 1; remoteGlobalIDString = {ident("app-target")}; remoteInfo = HomeLibrary;')
dep=obj('dependency',f'isa = PBXTargetDependency; target = {ident("app-target")}; targetProxy = {proxy};')
app_target=obj('app-target',f'isa = PBXNativeTarget; buildConfigurationList = {appconfig}; buildPhases = {array(app_phases)}; buildRules = (); dependencies = (); name = HomeLibrary; productName = HomeLibrary; productReference = {app}; productType = "com.apple.product-type.application";')
test_target=obj('test-target',f'isa = PBXNativeTarget; buildConfigurationList = {testconfig}; buildPhases = {array(test_phases)}; buildRules = (); dependencies = {array([dep])}; name = HomeLibraryTests; productName = HomeLibraryTests; productReference = {test}; productType = "com.apple.product-type.bundle.unit-test";')
obj('project',f'isa = PBXProject; attributes = {{ BuildIndependentTargetsInParallel = YES; LastUpgradeCheck = 1600; TargetAttributes = {{ {app_target} = {{ CreatedOnToolsVersion = 16.0; }}; {test_target} = {{ CreatedOnToolsVersion = 16.0; TestTargetID = {app_target}; }}; }}; }}; buildConfigurationList = {projconfig}; compatibilityVersion = "Xcode 14.0"; developmentRegion = en; hasScannedForEncodings = 0; knownRegions = (en, Base); mainGroup = {main}; productRefGroup = {products}; projectDirPath = ""; projectRoot = ""; targets = {array([app_target,test_target])};')
(project/'project.pbxproj').write_text('// !$*UTF8*$!\n{ archiveVersion = 1; classes = {}; objectVersion = 56; objects = {\n'+'\n'.join(objects)+f'\n}}; rootObject = {ident("project")}; }}\n')
scheme=ET.Element('Scheme',LastUpgradeVersion='1600',version='1.3')
refattrs={'BuildableIdentifier':'primary','BlueprintIdentifier':app_target,'BuildableName':'HomeLibrary.app','BlueprintName':'HomeLibrary','ReferencedContainer':'container:HomeLibrary.xcodeproj'}
build=ET.SubElement(scheme,'BuildAction',parallelizeBuildables='YES',buildImplicitDependencies='YES'); entries=ET.SubElement(build,'BuildActionEntries'); entry=ET.SubElement(entries,'BuildActionEntry',buildForTesting='YES',buildForRunning='YES',buildForProfiling='YES',buildForArchiving='YES',buildForAnalyzing='YES'); ET.SubElement(entry,'BuildableReference',**refattrs)
ta=ET.SubElement(scheme,'TestAction',buildConfiguration='Debug',selectedDebuggerIdentifier='Xcode.DebuggerFoundation.Debugger.LLDB',selectedLauncherIdentifier='Xcode.IDEFoundation.Launcher.LLDB',shouldUseLaunchSchemeArgsEnv='YES'); tr=ET.SubElement(ET.SubElement(ta,'Testables'),'TestableReference',skipped='NO'); ET.SubElement(tr,'BuildableReference',BuildableIdentifier='primary',BlueprintIdentifier=test_target,BuildableName='HomeLibraryTests.xctest',BlueprintName='HomeLibraryTests',ReferencedContainer='container:HomeLibrary.xcodeproj')
for action,mode in [('LaunchAction','Debug'),('ProfileAction','Release')]:
    act=ET.SubElement(scheme,action,buildConfiguration=mode); runnable=ET.SubElement(act,'BuildableProductRunnable',runnableDebuggingMode='0'); ET.SubElement(runnable,'BuildableReference',**refattrs)
ET.SubElement(scheme,'AnalyzeAction',buildConfiguration='Debug'); ET.SubElement(scheme,'ArchiveAction',buildConfiguration='Release',revealArchiveInOrganizer='YES')
shared=project/'xcshareddata/xcschemes'; shared.mkdir(parents=True,exist_ok=True); ET.indent(scheme); ET.ElementTree(scheme).write(shared/'HomeLibrary.xcscheme',encoding='utf-8',xml_declaration=True)
print('Generated Xcode project and shared scheme.')
