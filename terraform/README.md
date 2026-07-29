# Terraform — Cluster EKS

Provisiona la infraestructura de AWS sobre la que despliega el chart de Helm de este proyecto (`../chart/`): una VPC, un clúster EKS con un managed node group, el driver EBS CSI (para que los PVCs de `postgres.yaml`/`kafka.yaml`/`redis.yaml` puedan enlazarse), y el controlador `ingress-nginx` (`chart/templates/ingress.yaml` tiene fijo `ingressClassName: nginx`).

> **Esto no está conectado al CI.** `deploy.yml` asume que el clúster ya existe y apunta `KUBECONFIG` hacia él — no ejecuta este Terraform. Provisionar/destruir infraestructura real de AWS es un paso deliberado, manual, con costo asociado, no algo que deba pasar automáticamente en cada push.

---

## ⚠️ Advertencia de costo

Correr `terraform apply` aquí crea **recursos reales de AWS con costo**: un control plane de EKS (~$0.10/hr), de 1 a 3 nodos `t3.medium` (~$0.04/hr cada uno), un NAT gateway (~$0.045/hr + datos), un Network Load Balancer expuesto a internet para `ingress-nginx`, y volúmenes EBS gp3 para `postgres`/`kafka`/`redis`. Aproximadamente **$150–200/mes** si se deja corriendo continuamente. A diferencia de los hermanos AWS Lambda y Azure Functions de este portafolio (serverless, costo prácticamente cero cuando están inactivos), esta es infraestructura siempre activa. **Siempre ejecuta `terraform destroy` cuando termines de evaluarla.**

---

## Prerrequisitos

- [Terraform](https://developer.hashicorp.com/terraform/install) ≥ 1.5.7
- Credenciales de AWS con permiso para crear VPCs, clústeres EKS, roles IAM e instancias EC2
- `kubectl` y `helm` (para desplegar `../chart/` después)

## Uso

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars   # ajusta región/tamaño si hace falta
terraform init
terraform plan
terraform apply
```

Una vez aplicado, apunta `kubectl`/`helm` al nuevo clúster y despliega el chart:

```bash
$(terraform output -raw configure_kubectl)
helm upgrade --install product-service ../chart --namespace product-service --create-namespace
```

Para destruir todo:

```bash
terraform destroy
```

## Lo que esto *no* hace

- No construye ni publica imágenes Docker — eso lo hacen los jobs `docker-api`/`docker-web` de `ci.yml`.
- No configura DNS para `product-service.local` — o edita `/etc/hosts` para apuntar a la dirección del load balancer de `ingress-nginx` (`terraform output ingress_nginx_load_balancer_hint`), o usa un dominio real.
- No gestiona el secreto `KUBECONFIG`/entorno `production` de GitHub Actions que usa `deploy.yml` — eso es un paso manual (salida de `aws eks update-kubeconfig`, codificada en base64) si quieres que el CI despliegue sobre este clúster.

## Archivos

| Archivo | Propósito |
|---|---|
| `main.tf` | VPC (un solo NAT gateway) + clúster EKS + managed node group |
| `addons.tf` | Rol IAM del driver EBS CSI (Pod Identity), StorageClass `gp3` por defecto, release de Helm de `ingress-nginx` |
| `providers.tf` | Configuración de los providers `aws`/`kubernetes`/`helm`, usando el propio token de auth del clúster EKS |
| `variables.tf` / `outputs.tf` | Entradas y salidas — ver `terraform.tfvars.example` |
