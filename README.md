# MusicPipeBot
Telegram bot to download music from Spotify or Youtube Music. Currently supports only track downloading.
Uses [SpotDL](https://github.com/spotDL/spotify-downloader) for searching and downloading.

## How to run
Just add `config.json` (or `config-testing.json` for debugging) in project's folder containing the following structure:
```json
{
  "TelegramBotCredentials": {
    "Token": "here's your tg token"
  }
}
```
Docker Compose is available, so you can simply run `docker-compose up`.
## How to debug
In case you want to debug locally, there should be SpotDL and ffmpeg installed. You can find the installation guide [here](https://spotdl.readthedocs.io/en/latest/installation/) or in SpotDL's readme.
