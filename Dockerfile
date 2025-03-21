FROM nats:2.11-alpine
ENTRYPOINT ["docker-entrypoint.sh"]
CMD ["nats-server", "--jetstream"]
