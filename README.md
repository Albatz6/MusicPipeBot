# MusicPipeBot
Telegram bot to download music from Spotify or Youtube Music. Currently supports only track downloading.

## How to run
Just add `config.json` or `config-testing.json` for debugging in project's folder containing the following structure:
```json
{
  "TelegramBotCredentials": {
    "Token": "here's your tg token"
  }
}
```
Docker Compose is available, so you can simply run `docker-compose up`.
