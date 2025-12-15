#!/usr/bin/env bash

set -e

echo "🔄 Fetching all remotes..."
git fetch --all --prune

echo "🌿 Syncing remote branches..."

for remote in $(git branch -r | grep origin/ | grep -v '\->'); do
    branch="${remote#origin/}"

    if git show-ref --verify --quiet "refs/heads/$branch"; then
        echo "✔ Branch exists: $branch"
    else
        echo "➕ Creating branch: $branch"
        git branch "$branch" "$remote"
    fi
done

echo "⬇ Updating local branches..."

current_branch=$(git branch --show-current)

for branch in $(git branch --format='%(refname:short)'); do
    git checkout "$branch" >/dev/null
    git pull --ff-only origin "$branch" || echo "⚠ Could not update $branch"
done

git checkout "$current_branch" >/dev/null

echo "✅ All branches synced"
