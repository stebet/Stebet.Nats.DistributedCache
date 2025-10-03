FROM nats:2.12-alpine
ENTRYPOINT ["docker-entrypoint.sh"]
CMD ["nats-server", "--jetstream"]
