#!/bin/bash

# Specify the starting pattern for package names you want to update
package_prefix="PackageFilter"

# Get all project files in the solution
project_files=$(find . -name "*.csproj")

# Loop through each project file
for project in $project_files; do
    echo "Checking $project for packages to update..."

    # List packages with updates available that start with the specified prefix
    outdated_packages=$(dotnet list "$project" package --outdated | grep "$package_prefix" | awk '{print $2}')

    # Loop through each outdated package and update it
    for package in $outdated_packages; do
        echo "Updating $package in $project..."
        dotnet add "$project" package "$package"
    done
done

echo "Update process completed."
