module GiraffeTestApp.Routing

open System
open System.IO
open System.Collections.Generic
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe.HttpHandlers
open Giraffe.Middleware
open Giraffe.Razor.HttpHandlers
open Giraffe.Razor.Middleware
open GiraffeTestApp.Models
open Giraffe.Tasks
open Microsoft.AspNetCore.Http.Features
open System.Threading

let getFileNames (files : IFormFileCollection) =
    files 
    |> Seq.fold (fun acc file -> sprintf "%s\n%s" acc file.FileName) ""

let smallFileUploadHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            return!
                (match ctx.Request.HasFormContentType with
                | false -> setStatusCode 400 >=> text "Bad Request"
                | true -> getFileNames ctx.Request.Form.Files |> text
                ) next ctx
        }

let largeFileUploadHandler : HttpFunc -> HttpContext -> HttpFuncResult  =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let formFeature = ctx.Features.Get<IFormFeature>()
        
            let! form = formFeature.ReadFormAsync CancellationToken.None
            return! 
                (getFileNames form.Files |> text) next ctx
        }

let webApp : HttpHandler =
    choose [
        GET >=>
            choose [
                route "/" >=> razorHtmlView "Index" { Text = "Hello world, from Giraffe!" }
            ]
        POST >=> 
            choose [
                route "/small-upload" >=> smallFileUploadHandler
                route "/large-upload" >=> largeFileUploadHandler
            ]
        setStatusCode 404 >=> text "Not Found" ]