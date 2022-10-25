ifeq ($(NLBUILDROOT),)
  $(error NLBUILDROOT undefined.)
endif

ifeq ($(NLEXTERNALDIR),)
  $(error NLEXTERNALDIR undefined.)
endif

ifeq ($(NLEXTERNALDIR2),)
  $(error NLEXTERNALDIR2 undefined.)
endif

.PHONY: all
all: assembly installer distribute

# Same assembly is used to build both installers, but prepareAssembly.pl can only handle one .ism
# file at a time and .ism filename is defined in Makefile.x86 and Makefile.x64. So the second call
# to "make -f Makefile.x64 assemblyNoCopy" should not copy files again.
.PHONY: assembly
assembly:
	make -f Makefile.x86 assembly
	make -f Makefile.x64 assemblyNoCopy

.PHONY: installer
installer:
	make -f Makefile.x86 installer
	make -f Makefile.x64 installer

.PHONY: distribute
distribute:
	make -f Makefile.x86 distribute
	make -f Makefile.x64 distribute

.PHONY: clean
clean:
	make -f Makefile.x86 clean
	make -f Makefile.x64 clean
	