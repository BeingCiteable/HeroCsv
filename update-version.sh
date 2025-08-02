#!/bin/bash

# Update version script for FastCsv
# Usage: ./update-version.sh <version> [--create-tag] [--push]

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if version is provided
if [ $# -lt 1 ]; then
    echo "Usage: $0 <version> [--create-tag] [--push]"
    echo "Example: $0 1.0.1"
    echo "Example: $0 1.0.1 --create-tag --push"
    exit 1
fi

VERSION=$1
CREATE_TAG=false
PUSH_TAG=false

# Parse additional arguments
for arg in "${@:2}"; do
    case $arg in
        --create-tag)
            CREATE_TAG=true
            ;;
        --push)
            PUSH_TAG=true
            ;;
        *)
            echo "Unknown argument: $arg"
            exit 1
            ;;
    esac
done

# Validate version format
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+)?$ ]]; then
    echo -e "${RED}Invalid version format. Expected: X.Y.Z or X.Y.Z-suffix (e.g., 1.0.0 or 1.0.0-preview1)${NC}"
    exit 1
fi

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROPS_FILE="$SCRIPT_DIR/Directory.Build.props"

# Check if Directory.Build.props exists
if [ ! -f "$PROPS_FILE" ]; then
    echo -e "${RED}Directory.Build.props not found at: $PROPS_FILE${NC}"
    exit 1
fi

# Extract version parts (remove prerelease suffix for FileVersion)
VERSION_PARTS=$(echo $VERSION | cut -d'-' -f1)
FILE_VERSION="${VERSION_PARTS}.0"

# Update version in Directory.Build.props
sed -i.bak "s|<Version>.*</Version>|<Version>$VERSION</Version>|" "$PROPS_FILE"
sed -i.bak "s|<FileVersion>.*</FileVersion>|<FileVersion>$FILE_VERSION</FileVersion>|" "$PROPS_FILE"
sed -i.bak "s|<AssemblyVersion>.*</AssemblyVersion>|<AssemblyVersion>$FILE_VERSION</AssemblyVersion>|" "$PROPS_FILE"

# Remove backup file
rm -f "${PROPS_FILE}.bak"

echo -e "${GREEN}✅ Updated version to $VERSION in Directory.Build.props${NC}"

# Create git tag if requested
if [ "$CREATE_TAG" = true ]; then
    TAG_NAME="v$VERSION"
    
    # Check if we're in a git repository
    if [ ! -d ".git" ]; then
        echo -e "${YELLOW}Warning: Not in a git repository. Skipping tag creation.${NC}"
    else
        # Check if tag already exists
        if git tag -l "$TAG_NAME" | grep -q "$TAG_NAME"; then
            echo -e "${RED}Tag $TAG_NAME already exists${NC}"
            exit 1
        fi
        
        # Create the tag
        git tag -a "$TAG_NAME" -m "Release version $VERSION"
        echo -e "${GREEN}✅ Created git tag: $TAG_NAME${NC}"
        
        # Push if requested
        if [ "$PUSH_TAG" = true ]; then
            echo -e "${YELLOW}Pushing tag to origin...${NC}"
            git push origin "$TAG_NAME"
            echo -e "${GREEN}✅ Pushed tag $TAG_NAME to origin${NC}"
        else
            echo -e "${CYAN}💡 To push the tag, run: git push origin $TAG_NAME${NC}"
        fi
    fi
fi

echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo -e "${CYAN}1. Commit the version change: git add Directory.Build.props && git commit -m \"chore: bump version to $VERSION\"${NC}"
echo -e "${CYAN}2. Push the commit: git push${NC}"
if [ "$CREATE_TAG" = false ]; then
    echo -e "${CYAN}3. Create a release on GitHub with tag v$VERSION${NC}"
fi