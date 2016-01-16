xbuild /p:Configuration=Release
mkdir -p bin
echo 'Copying Binaries to bin folder'
cp TinyBeanVMAssemblerCLI/bin/Release/TinyBeanVMAssemblerCLI.exe bin/tinybeanassembler.exe
cp TinyBeanVMMachineCLI/bin/Release/TinyBeanVMMachineCLI.exe bin/tinybeanmachine.exe