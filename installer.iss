[Setup]
AppName=TankExporter
AppVersion=1.0
DefaultDirName={pf}\TankExporter
OutputDir=Output
OutputBaseFilename=TankExporterSetup

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\TankExporter"; Filename: "{app}\TankExporter.exe"