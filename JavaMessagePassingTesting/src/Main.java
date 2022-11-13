import java.net.*;
import java.io.*;

public class Main {
    public static void main(String[] args)
    {
        if (args.length < 2)
        {
            if (args.length < 1)
            {
                System.out.println("No IP Address given");
            }
            System.out.println("No port given");

            return;
        }

        String host = args[0];
        int port = Integer.parseInt(args[1]);
        try (Socket socket = new Socket(host, port)) {
            OutputStream output = socket.getOutputStream();
            PrintWriter writer = new PrintWriter(output, true);
            InputStream input = socket.getInputStream();
            BufferedReader reader = new BufferedReader(new InputStreamReader(input));

            boolean[][] data = new boolean[1080][1920];
            byte[] bData = new byte[1920*1080/8];

            int numBytesRead = 0;
            while (numBytesRead < bData.length) // This must be in a loop because read may not receive all the data at once
            {
                numBytesRead += input.read(bData, numBytesRead, bData.length - numBytesRead);
            }

            int dataWidth = 1920;
            for (int i = 0; i < 1080; i++)
            {
                for (int j = 0; j < dataWidth; j += 8)
                {
                    // unpack byte into 8 booleans
                    Byte boolSet = bData[(i*dataWidth + j)/8];
                    data[i][j] = (boolSet & 0x80) == 0x80;
                    data[i][j+1] = (boolSet & 0x40) == 0x40;
                    data[i][j+2] = (boolSet & 0x20) == 0x20;
                    data[i][j+3] = (boolSet & 0x10) == 0x10;
                    data[i][j+4] = (boolSet & 0x08) == 0x08;
                    data[i][j+5] = (boolSet & 0x04) == 0x04;
                    data[i][j+6] = (boolSet & 0x02) == 0x02;
                    data[i][j+7] = (boolSet & 0x01) == 0x01;
                }
            }

            File fOut = new File("javaClientOutput.txt");
            try(BufferedWriter out = new BufferedWriter(new FileWriter(fOut)))
            {

                for (int i = 0; i < 1080; i++) {
                    for (int j = 0; j < 1920; j++) {
                        if (data[i][j]) {
                            out.write("1");
                        } else {
                            out.write("0");
                        }
                        out.write(" "); // notepad can't view this file correctly
                    }
                    out.write("\n");
                }
            }
            catch (IOException e)
            {
                System.out.println("ERROR: could not open java file: " + e.toString());
            }
        }
        catch (IOException ex) {
            System.out.println("ERROR: " + ex.getMessage());
        }
    }
}