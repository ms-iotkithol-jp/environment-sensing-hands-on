# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for
# full license information.

import time
import os
import sys
import asyncio
from six.moves import input
import threading
from azure.iot.device import IoTHubModuleClient, MethodResponse
from blobfileuploader import BlobFileUploader
import cv2
import datetime

BLOB_ON_EDGE_MODULE = ""
BLOB_ON_EDGE_ACCOUNT_NAME = ""
BLOB_ON_EDGE_ACCOUNT_KEY = ""
PHOTO_CONTAINER_NAME = ""
PHOTO_DATA_FOLDER = ""
EDGE_DEVICEID = ""

uploadCycleSecKey = "upload_cycle_sec"
uploadCycleSec = 10
quitFlag = False
uploadStared = False

async def main(videoPath, fileUploader):
    global uploadCycleSecKey, uploadCycleSec, quitFlag
    try:
        if not sys.version >= "3.5.3":
            raise Exception( "The sample requires python 3.5.3+. Current version of Python: %s" % sys.version )
        print ( "IoT Hub Client for Python" )

        # The client object is used to interact with your Azure IoT hub.
        module_client = IoTHubModuleClient.create_from_edge_environment()

        # connect the client.
        module_client.connect()

        currentTwin = module_client.get_twin()
        dtwin = currentTwin['desired']
        if uploadCycleSecKey in dtwin:
            uploadCycleSec = dtwin[uploadCycleSecKey]

        # define behavior for receiving an input message on input1
        def twin_patch_listener(module_client, param_lock):
            global uploadCycleSec
            print("twin patch listener started.")
            while True:
                data = module_client.receive_twin_desired_properties_patch()  # blocking call
                print("Received desired properties updated.")
                if uploadCycleSecKey in data:
                    param_lock.acquire()
                    uploadCycleSec = data[uploadCycleSecKey]
                    param_lock.release()
                    print("Updated {}={}".format(uploadCycleSecKey, uploadCycleSec))
                param_lock.acquire()
                isBreak = quitFlag
                param_lock.release()
                if isBreak:
                    print("twin patch listener will be finished")
                    break

        async def upload_photo_handler(videoPath, uploader, param_lock):
            global PHOTO_DATA_FOLDER, uploadCycleSec, uploadStared
            await uploader.initialize()
            print("upload photo handler started.")
            try:
                print("creating VideoStream")
#                cap = cv2.VideoStream(int(videoPath))
                cap = cv2.VideoCapture(int(videoPath))
                print("...Created")
                time.sleep(1)
                if cap.isOpened():
                    print("VideoCapture has been opened")
                else:
                    print("VideoCapture has not been opened")
                while True:
                    param_lock.acquire()
                    sleepTime = uploadCycleSec
                    isUpload = uploadStared
                    param_lock.release()

                    time.sleep(sleepTime)
                    if isUpload:
                        now = datetime.datetime.now()
                        photoFileName = 'photo-{0:%Y%m%d%H%M%S%f}'.format(now) + '.jpg'
                        filename = os.path.join(PHOTO_DATA_FOLDER, photoFileName)
                        print("Try to take photo - name={}".format(filename))
                        ret, frame = cap.read()
                        cv2.imwrite(filename, frame)
                        print("Saved photo file")
                        await uploader.uploadFile(filename)
                        os.remove(filename)
                    else:
                        print("Waiting for start")
                    param_lock.acquire()
                    isBreak = quitFlag
                    param_lock.release()
                    if isBreak:
                        print("upload photo handler will be finished")
                        break

            except Exception as error:
                print('upload photo handler exception - {}'.format(error)) 

        def direct_method_listener(module_client, param_lock):
            global uploadStared
            while True:
                try:
                    print("waiting for method invocation...")
                    methodRequest = module_client.receive_method_request()
                    print("received method invocation - '{}'({})".format(methodRequest.name, methodRequest.payload))
                    response = {}
                    response_status = 200
                    if methodRequest.name == "Start":
                        param_lock.acquire()
                        uploadStared = True
                        param_lock.release()
                        response['message'] ="Upload started."
                        print("Received - Start order")
                    elif methodRequest.name == "Stop":
                        param_lock.acquire()
                        uploadStared = False
                        param_lock.release()
                        response['message'] ="Upload stopped."
                        print("Received - Stop order")
                    else:
                        response['message'] = "bad method name"
                        response_status = 404
                        print("Bad Method Request!")
                    methodResponse = MethodResponse(methodRequest.request_id, response_status, payload=response)
                    module_client.send_method_response(methodResponse)
                except Exception as error:
                    print("exception happens - {}".format(error))

        param_lock = threading.Lock()

        twinThread = threading.Thread(target=twin_patch_listener, args=(module_client, param_lock))
        twinThread.daemon = True
        twinThread.start()
        
        methodThread = threading.Thread(target=direct_method_listener, args=(module_client, param_lock))
        methodThread.daemon = True
        methodThread.start()

#        uploadPhotoThread = threading.Thread(target=upload_photo_handler, args=(videoPath, fileUploader, param_lock))
#        uploadPhotoThread.daemon = True
#        uploadPhotoThread.start()

        # Schedule task for Photo Uploader
        listeners = asyncio.gather(upload_photo_handler(videoPath, fileUploader, param_lock))

        print ( "The sample is now waiting for direct method and desired twin update. ")

        def stdin_listener():
            while True:
                try:
                    selection = input("Press Q to quit\n")
                    if selection == "Q" or selection == "q":
                        print("Quitting...")
                        param_lock.acquire()
                        quitFlag = True
                        param_lock.release()
                        break
                except:
                    time.sleep(10)

        # Run the stdin listener in the event loop
        loop = asyncio.get_event_loop()
        user_finished = loop.run_in_executor(None, stdin_listener)

        # Wait for user to indicate they are done listening for messages
        await user_finished

        # Cancel listening
        listeners.cancel()

        # uploadPhotoThread.join()
        methodThread.join()
        twinThread.join()

        # Finally, disconnect
        module_client.disconnect()

    except Exception as e:
        print ( "Unexpected error %s " % e )
        raise

if __name__ == "__main__":

    BLOB_ON_EDGE_MODULE = os.environ['BLOB_ON_EDGE_MODULE']
    BLOB_ON_EDGE_ACCOUNT_NAME = os.environ['BLOB_ON_EDGE_ACCOUNT_NAME']
    BLOB_ON_EDGE_ACCOUNT_KEY = os.environ['BLOB_ON_EDGE_ACCOUNT_KEY']
    PHOTO_CONTAINER_NAME=os.environ['PHOTO_CONTAINER_NAME']
    PHOTO_DATA_FOLDER = os.environ['PHOTO_DATA_FOLDER']
    EDGE_DEVICEID = os.environ['IOTEDGE_DEVICEID']

    fileUploader = BlobFileUploader(BLOB_ON_EDGE_MODULE, BLOB_ON_EDGE_ACCOUNT_NAME, BLOB_ON_EDGE_ACCOUNT_KEY, PHOTO_CONTAINER_NAME, EDGE_DEVICEID)
    loop = asyncio.get_event_loop()
    loop.run_until_complete(main('0', fileUploader))
    loop.close()
