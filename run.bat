@ECHO off
start .\CableCloud\bin\Debug\netcoreapp3.1\CableCloud.exe ".\Resources\Config.xml"
TIMEOUT /T 1
start .\LRM\bin\Debug\netcoreapp3.1\LRM.exe ".\LRM\ConfigCore.xml"
TIMEOUT /T 1
start .\LRM\bin\Debug\netcoreapp3.1\LRM.exe ".\LRM\ConfigS1.xml"
TIMEOUT /T 1
start .\LRM\bin\Debug\netcoreapp3.1\LRM.exe ".\LRM\ConfigS2.xml"
TIMEOUT /T 1
start .\RC\bin\Debug\netcoreapp3.1\Dijkstra.exe ".\RC\Config.txt"
TIMEOUT /T 1
start .\RC\bin\Debug\netcoreapp3.1\Dijkstra.exe ".\RC\ConfigS1.txt"
TIMEOUT /T 1
start .\RC\bin\Debug\netcoreapp3.1\Dijkstra.exe ".\RC\ConfigS2.txt"
TIMEOUT /T 1
start .\CC\bin\Debug\netcoreapp3.1\CC.exe "./Resources/CC_config.xml"
TIMEOUT /T 1
start .\CC\bin\Debug\netcoreapp3.1\CC.exe "./Resources/CC_S1_config.xml"
TIMEOUT /T 1
start .\CC\bin\Debug\netcoreapp3.1\CC.exe "./Resources/CC_S2_config.xml"
TIMEOUT /T 1
start .\NCC\NCC\bin\Debug\netcoreapp3.1\NCC.exe -- "./Resources/NCCConfigExample.xml"
TIMEOUT /T 1
start .\Host\bin\Debug\netcoreapp3.1\Host.exe -- "./Resources/HostConfigExample.xml"
TIMEOUT /T 1
start .\Host\bin\Debug\netcoreapp3.1\Host.exe -- "./Resources/Host2ConfigExample.xml"
TIMEOUT /T 1
start .\Host\bin\Debug\netcoreapp3.1\Host.exe -- "./Resources/Host3ConfigExample.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node1_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node2_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node3_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node4_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node5_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node6_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node7_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node8_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node9_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node10_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node11_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node12_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node13_config.xml"
TIMEOUT /T 1
start .\NetworkNode\bin\Debug\netcoreapp3.1\NetworkNode.exe "./Resources/node14_config.xml"
pause