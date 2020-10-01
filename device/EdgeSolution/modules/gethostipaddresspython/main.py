# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for
# full license information.

import time
import os
import sys
import asyncio
from six.moves import input
import threading
import subprocess
from subprocess import PIPE
from azure.iot.device.aio import IoTHubModuleClient

async def main():
    try:
        if not sys.version >= "3.5.3":
            raise Exception( "The sample requires python 3.5.3+. Current version of Python: %s" % sys.version )
        print ( "IoT Hub Client for Python" )

        # The client object is used to interact with your Azure IoT hub.
        module_client = IoTHubModuleClient.create_from_edge_environment()

        # connect the client.
        await module_client.connect()

        print ( 'Checking host IP address...')
        netinfo = {}
        for netname in ["eth0", "wlan0"]:
            proc = subprocess.run("/sbin/ip address show {}".format(netname), shell=True, stdout=PIPE)
            l = str(proc.stdout)
            print(str(l))
            if l.find('inet') > 0:
                ipaddress = l[l.find('inet'):].split()[1]
                netinfo[netname] = ipaddress
                print('Found - {}'.format(ipaddress))

        reported_properties = {"network-info": netinfo}
        print("Setting reported network info to {}".format(reported_properties["network-info"]))
        await module_client.patch_twin_reported_properties(reported_properties)


        # define behavior for receiving an input message on input1
        # define behavior for halting the application
        def stdin_listener():
            while True:
                try:
                    selection = input("Press Q to quit\n")
                    if selection == "Q" or selection == "q":
                        print("Quitting...")
                        break
                except:
                    time.sleep(10)

        # Schedule task for C2D Listener

        print ( "The sample is now waiting for messages. ")

        # Run the stdin listener in the event loop
        loop = asyncio.get_event_loop()
        user_finished = loop.run_in_executor(None, stdin_listener)

        # Wait for user to indicate they are done listening for messages
        await user_finished

        # Cancel listening

        # Finally, disconnect
        await module_client.disconnect()

    except Exception as e:
        print ( "Unexpected error %s " % e )
        raise

if __name__ == "__main__":
    loop = asyncio.get_event_loop()
    loop.run_until_complete(main())
    loop.close()

    # If using Python 3.7 or above, you can use following code instead:
    # asyncio.run(main())