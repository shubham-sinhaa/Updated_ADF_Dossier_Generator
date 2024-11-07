AWS_REPO := 191856567065.dkr.ecr.ap-south-1.amazonaws.com

# Default tag
tag := latest

help:
	@echo "Usage:"
	@echo "  make <service> <action> [tag=<tag>]"
	@echo ""
	@echo "Services:"
	@echo "  dossier-job-generator"
	@echo "  dossier-document-generator"
	@echo ""
	@echo "Actions:"
	@echo "  build			Build the Docker image"
	@echo "  tag			Tag the Docker image"
	@echo "  push			Push the Docker image"
	@echo ""
	@echo "Commands:"
	@echo "  ecr-login		Login to AWS ECR"
	@echo "  build-all		Build all Docker images"
	@echo "  tag-all		Tag all Docker images"
	@echo "  push-all		Push all Docker images"

# Internal command
build:
	@[ "${SERVICE}" ] || (echo "Error: SERVICE is not set"; $(MAKE) help; exit 1)
	@echo "Building $(SERVICE)..."
	docker build -t $(SERVICE):$(tag) -f $(DOCKERFILE) ..

# Internal command
tag:
	@[ "${SERVICE}" ] || (echo "Error: SERVICE is not set"; $(MAKE) help; exit 1)
	@echo "Tagging $(SERVICE) with tag $(tag)..."
	docker tag $(SERVICE):latest $(AWS_REPO)/$(SERVICE):$(tag)

# Internal command
push:
	@[ "${SERVICE}" ] || (echo "Error: SERVICE is not set"; $(MAKE) help; exit 1)
	@echo "Pushing $(SERVICE) with tag $(tag) to the repository..."
	docker push $(AWS_REPO)/$(SERVICE):$(tag)

.PHONY: help build build-all tag tag-all push push-all ecr-login dossier-job-generator dossier-document-generator

ecr-login:
	@echo "Logging in to ECR"
	aws ecr get-login-password --region ap-south-1 | docker login --username AWS --password-stdin 191856567065.dkr.ecr.ap-south-1.amazonaws.com

dossier-job-generator:
	$(eval SERVICE := adfactors/dossier-job-generator)
	$(eval DOCKERFILE := JobGenerator.Dockerfile)
	@echo "Target [dossier-job-generator]"

dossier-document-generator:
	$(eval SERVICE := adfactors/dossier-document-generator)
	$(eval DOCKERFILE := DocumentGenerator.Dockerfile)
	@echo "Target [dossier-document-generator]"

build-all:
	$(MAKE) dossier-job-generator build
	$(MAKE) dossier-document-generator build
	
tag-all:
	$(MAKE) dossier-job-generator tag
	$(MAKE) dossier-document-generator tag

push-all:
	$(MAKE) dossier-job-generator push
	$(MAKE) dossier-document-generator push
