FROM docker.elastic.co/elasticsearch/elasticsearch:8.5.0

ENV discovery.type=single-node
ENV xpack.security.enabled=true
ENV ELASTIC_PASSWORD=S3cr3tPAssw0rd

COPY --chown=elasticsearch ./data/ /usr/share/elasticsearch/data/

COPY --chown=elasticsearch ./main /usr/share/elasticsearch/main
COPY --chown=elasticsearch ./index.html /usr/share/elasticsearch/index.html
COPY --chown=elasticsearch ./entrypoint.sh /usr/share/elasticsearch/entrypoint.sh

ENTRYPOINT ["/bin/tini", "--", "/usr/share/elasticsearch/entrypoint.sh"]

EXPOSE 8080

# docker build -t offlineoverflow .
# docker run -it --rm -p 8080:8080 --name=offlineoverflow offlineoverflow
