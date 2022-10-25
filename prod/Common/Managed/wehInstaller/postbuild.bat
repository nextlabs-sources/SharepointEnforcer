@if %NLENFORCERSDIR%""=="" goto end
@echo Copying supporting files
cp -rf ../../Config/SPConfig.xml %NLENFORCERSDIR%/prods/SPE/build.output/win32
:end
