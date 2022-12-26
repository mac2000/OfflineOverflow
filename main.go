package main

import (
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
)

func main() {
	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/" || r.URL.Path == "/index.html" {
			file, err := os.Open("index.html")
			if err != nil {
				w.WriteHeader(500)
				fmt.Fprintf(w, "%s", err.Error())
				return
			}
			defer file.Close()
			w.Header().Set("Content-Type", "text/html")
			io.Copy(w, file)
			return
		}

		r.Host = "localhost:9200"
		r.URL.Scheme = "http"
		r.URL.Host = "localhost:9200"
		r.RequestURI = ""
		r.SetBasicAuth("elastic", "S3cr3tPAssw0rd")

		res, err := http.DefaultClient.Do(r)
		if err != nil {
			w.WriteHeader(500)
			fmt.Fprintf(w, "%s", err.Error())
			return
		}

		for k, vv := range res.Header {
			for _, v := range vv {
				w.Header().Add(k, v)
			}
		}

		w.WriteHeader(res.StatusCode)
		io.Copy(w, res.Body)
	})

	log.Fatal(http.ListenAndServe(":8080", nil))
}
