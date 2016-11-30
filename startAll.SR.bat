cd /d %~dp0

pushd ..\Assemblies\ElasticSearchVNext\Server\bin\
start elasticsearch.bat
popd

pushd ..\Assemblies\Cassandra\Server\bin\
start cassandra.bat
popd

start "ServiceRunner" "..\Tools\ServiceRunner\ServiceRunner.exe" "_StartAllConfigs\startAll.SR.yaml" "-startAllServices"