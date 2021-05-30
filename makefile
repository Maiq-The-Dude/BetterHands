SHELL            = /bin/bash

GIT_BRANCH       = $(shell git rev-parse --abbrev-ref HEAD)
GIT_HASH         = $(shell git rev-parse HEAD)

PROJECT          = BetterHands
PACKAGE          = BetterHands.deli
CONTENTS         = manifest.json hookgen.conf BetterHands.dll

CONFIG           = Release
FRAMEWORK        = net35
BUILD_PROPERTIES = /p:RepositoryBranch="$(GIT_BRANCH)" /p:RepositoryCommit="$(GIT_HASH)"

.PHONY: all build clean

all: clean $(PACKAGE)

build:
	dotnet build --configuration $(CONFIG) --framework $(FRAMEWORK) $(BUILD_PROPERTIES)

$(PACKAGE): build
	zip -9j $@ $(addprefix $(PROJECT)/bin/$(CONFIG)/$(FRAMEWORK)/,$(CONTENTS))

clean:
	rm -f $(PACKAGE)
