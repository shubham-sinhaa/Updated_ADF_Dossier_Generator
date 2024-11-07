#!/bin/bash

# Run the script using source to export the variable to the current shell session
#eg. source ./update_host_ip.sh

# Get the IP address of the host machine from within WSL2
HOST_IP=$(cat /etc/resolv.conf | grep nameserver | awk '{print $2}')

# Export the IP address as an environment variable
export HOST_INTERNAL_IP=$HOST_IP

# Print the IP address for verification
echo "Host IP is $HOST_INTERNAL_IP"