
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE" "-keyfile:Gurux.Serial.snk"
ASSEMBLY = bin/Debug/Gurux.Serial.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug/

GURUX_SERIAL_DLL_MDB_SOURCE=bin/Debug/Gurux.Serial.dll.mdb
GURUX_SERIAL_DLL_MDB=$(BUILD_DIR)/Gurux.Serial.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE" "-keyfile:Gurux.Serial.snk"
ASSEMBLY = bin/Release/Gurux.Serial.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release/

GURUX_SERIAL_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(GURUX_SERIAL_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(GURUX_SERIAL_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	AvailableMediaSettings.cs \
	GXSerial.cs \
	Properties/AssemblyInfo.cs \
	Resources.Designer.cs \
	Settings.cs \
	Shared/GXSynchronousMediaBase.cs \
	Shared/PropertiesForm.cs \
	Shared/PropertiesForm.Designer.cs \
	Settings.Designer.cs 

DATA_FILES = 

RESOURCES = \
	Resources.resx,Gurux.Serial.Resources.resources \
	Settings.resx,Gurux.Serial.Settings.resources \
	Shared/PropertiesForm.resx,Gurux.Shared.PropertiesForm.resources 

EXTRAS = \
	Gurux.Serial.snk \
	gurux.serial.pc.in 

REFERENCES =  \
	$(GURUXCOMMON_LIBS) \
	System \
	System.Core \
	System.Drawing \
	System.Windows.Forms \
	System.Xml.Linq \
	System.Data.DataSetExtensions \
	System.Data \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

GURUX_SERIAL_PC = $(BUILD_DIR)/gurux.serial.pc

$(eval $(call emit-deploy-wrapper,GURUX_SERIAL_PC,gurux.serial.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
