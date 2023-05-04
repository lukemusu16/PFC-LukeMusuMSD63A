FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build-env
WORKDIR /app

COPY classApp/classApp/classApp.csproj classApp/
RUN dotnet restore classApp/classApp.csproj

COPY . ./
RUN dotnet publish classApp -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:3.1
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
EXPOSE 80
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "classApp.dll"]


#From python:3.11-slim-buster
#WORKDIR /app
#COPY pythonTranscribe/requirements.txt pythonTranscribe/
#RUN pip3 install -r pythonTranscribe/requirements.txt
#RUN apt-get update
#RUN apt-get install ffmpeg libsm6 libxext6  -y
#RUN apt-get install net-tools
#COPY . ./
#CMD ["python3", "-u". "WEB_APP.py"]




 
