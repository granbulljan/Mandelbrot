using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using OpenCL.Net;



class OpenCLHelp
{
    private Context _context;
    private Device _device;

    public OpenCLHelp()
    {
        ErrorCode error;
        Platform[] platforms = Cl.GetPlatformIDs(out error);
        List<Device> devicesList = new List<Device>();

        CheckErr(error, "Cl.GetPlatformIDs");

        foreach (Platform platform in platforms)
        {
            string platformName = Cl.GetPlatformInfo(platform, PlatformInfo.Name, out error).ToString();
            Console.WriteLine("Platform: " + platformName);
            CheckErr(error, "Cl.GetPlatformInfo");
            //We will be looking only for GPU devices
            foreach (Device device in Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error))
            {
                CheckErr(error, "Cl.GetDeviceIDs");
                Console.WriteLine("Device: " + device.ToString());
                devicesList.Add(device);
            }
        }

        if (devicesList.Count <= 0)
        {
            Console.WriteLine("No devices found.");
            return;
        }

        _device = devicesList[0];

        if (Cl.GetDeviceInfo(_device, DeviceInfo.ImageSupport,
                  out error).CastTo<Bool>() == Bool.False)
        {
            Console.WriteLine("No image support.");
            return;
        }
        _context
     = Cl.CreateContext(null, 1, new[] { _device }, ContextNotify,
    IntPtr.Zero, out error);    //Second parameter is amount of devices
        CheckErr(error, "Cl.CreateContext");
    }

    public Bitmap GPUMandel(Bitmap inputBitmap)
    {
        ErrorCode error;
        //Load and compile kernel source code.
        string programPath = System.Environment.CurrentDirectory + "/../../program.cl";
        //The path to the source file may vary

        if (!System.IO.File.Exists(programPath))
        {
            Console.WriteLine("Program doesn't exist at path " + programPath);
            return inputBitmap;
        }

        string programSource = System.IO.File.ReadAllText(programPath);

        using (Program program = Cl.CreateProgramWithSource(_context, 1, new[] { programSource }, null, out error))
        {
            CheckErr(error, "Cl.CreateProgramWithSource");
            //Compile kernel source
            error = Cl.BuildProgram(program, 1, new[] { _device }, string.Empty, null, IntPtr.Zero);
            CheckErr(error, "Cl.BuildProgram");
            //Check for any compilation errors
            if (Cl.GetProgramBuildInfo(program, _device, ProgramBuildInfo.Status, out error).CastTo<BuildStatus>()
                != BuildStatus.Success)
            {
                CheckErr(error, "Cl.GetProgramBuildInfo");
                Console.WriteLine("Cl.GetProgramBuildInfo != Success");
                Console.WriteLine(Cl.GetProgramBuildInfo(program, _device, ProgramBuildInfo.Log, out error));
                return inputBitmap;
            }
            //Create the required kernel (entry function)
            Kernel kernel = Cl.CreateKernel(program, "Mandelbrot", out error);
            CheckErr(error, "Cl.CreateKernel");

            
            //Unmanaged output image's raw RGBA byte[] array
            BitmapData bitmapData = inputBitmap.LockBits(new Rectangle(0, 0, inputBitmap.Width, inputBitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int inputImgBytesSize = bitmapData.Stride * bitmapData.Height;
            int inputImgWidth = inputBitmap.Width;
            int inputImgHeight = inputBitmap.Height;
            int intPtrSize = Marshal.SizeOf(typeof(IntPtr));

            OpenCL.Net.ImageFormat clImageFormat = new OpenCL.Net.ImageFormat(ChannelOrder.RGBA, ChannelType.Unsigned_Int8);
            //OpenCL.Net.ImageFormat clImageFormat = new OpenCL.Net.ImageFormat(ChannelOrder.RGBA, ChannelType.Float);


            byte[] outputByteArray = new byte[inputImgBytesSize];
            //Allocate OpenCL image memory buffer
            Mem outputImage2DBuffer = (Mem)Cl.CreateImage2D(_context, MemFlags.CopyHostPtr |
                MemFlags.WriteOnly, clImageFormat, (IntPtr)inputImgWidth,
                (IntPtr)inputImgHeight, (IntPtr)0, outputByteArray, out error);
            CheckErr(error, "Cl.CreateImage2D output");
            //Pass the memory buffers to our kernel function
            error = Cl.SetKernelArg(kernel, 0, (IntPtr)intPtrSize, outputImage2DBuffer);
            CheckErr(error, "Cl.SetKernelArg");

            //Create a command queue, where all of the commands for execution will be added
            CommandQueue cmdQueue = Cl.CreateCommandQueue(_context, _device, (CommandQueueProperties)0, out error);
            CheckErr(error, "Cl.CreateCommandQueue");
            Event clevent;
            //Copy input image from the host to the GPU.
            IntPtr[] originPtr = new IntPtr[] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };    //x, y, z
            IntPtr[] regionPtr = new IntPtr[] { (IntPtr)inputImgWidth, (IntPtr)inputImgHeight, (IntPtr)1 };    //x, y, z
            IntPtr[] workGroupSizePtr = new IntPtr[] { (IntPtr)inputImgWidth, (IntPtr)inputImgHeight, (IntPtr)1 };

            error = Cl.EnqueueWriteImage(cmdQueue, outputImage2DBuffer, Bool.True,
               originPtr, regionPtr, (IntPtr)0, (IntPtr)0, outputByteArray, 0, null, out clevent);

            CheckErr(error, "Cl.EnqueueWriteImage");
            //Execute our kernel (OpenCL code)
            error = Cl.EnqueueNDRangeKernel(cmdQueue, kernel, 2, null, workGroupSizePtr, null, 0, null, out clevent);
            CheckErr(error, "Cl.EnqueueNDRangeKernel");
            //Wait for completion of all calculations on the GPU.
            error = Cl.Finish(cmdQueue);
            CheckErr(error, "Cl.Finish");
            //Read the processed image from GPU to raw RGBA data byte[] array
            error = Cl.EnqueueReadImage(cmdQueue, outputImage2DBuffer, Bool.True, originPtr, regionPtr,
                                        (IntPtr)0, (IntPtr)0, outputByteArray, 0, null, out clevent);
            CheckErr(error, "Cl.clEnqueueReadImage");
            //Clean up memory
            Cl.ReleaseKernel(kernel);
            Cl.ReleaseCommandQueue(cmdQueue);

            //Cl.ReleaseMemObject(inputImage2DBuffer);
            Cl.ReleaseMemObject(outputImage2DBuffer);
            //Get a pointer to our unmanaged output byte[] array
            GCHandle pinnedOutputArray = GCHandle.Alloc(outputByteArray, GCHandleType.Pinned);
            IntPtr outputBmpPointer = pinnedOutputArray.AddrOfPinnedObject();
            //Create a new bitmap with processed data and save it to a file.

            Bitmap outputBitmap = new Bitmap(inputImgWidth, inputImgHeight,
                  bitmapData.Stride, PixelFormat.Format32bppArgb, outputBmpPointer);

            return outputBitmap;

            //outputBitmap.Save(outputImagePath, System.Drawing.Imaging.ImageFormat.Png);
            //pinnedOutputArray.Free();
        }
    }

    private void CheckErr(ErrorCode err, string name)
    {
        if (err != ErrorCode.Success)
        {
            Console.WriteLine("ERROR: " + name + " (" + err.ToString() + ")");
        }
    }
    private void ContextNotify(string errInfo, byte[] data, IntPtr cb, IntPtr userData)
    {
        Console.WriteLine("OpenCL Notification: " + errInfo);
    }
}

