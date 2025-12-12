#!/usr/bin/env bash

REMOTE_USER="ubuntu"
REMOTE_HOST="54.221.180.40"
REMOTE_DIR="~/GameServer/LinuxBuild"   
SSH_KEY="$HOME/.ssh/FWA_Key.pem"
LOCAL_BUILD_DIR="/c/Users/mnowi/Desktop/Game/Netcode/LinuxBuild"



echo "Deploying from ${LOCAL_BUILD_DIR} to ${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_DIR}"

# SCP entire build folder
scp -C -i "${SSH_KEY}" -r "${LOCAL_BUILD_DIR}/" "${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_DIR}"

if [ $? -ne 0 ]; then
    echo "scp failed, aborting."
    exit 1
fi

echo "Upload complete. Restarting server..."


# Restart server process
ssh -i "${SSH_KEY}" "${REMOTE_USER}@${REMOTE_HOST}" "cd ${REMOTE_DIR} && ./server.sh restart"

echo "Done."

